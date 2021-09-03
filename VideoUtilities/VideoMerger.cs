using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace VideoUtilities
{
    public class VideoMerger : BaseClass<string>
    {
        private readonly List<MetadataClass> metadataClasses = new List<MetadataClass>();
        private readonly TimeSpan totalDuration;
        private readonly string tempFile;
        private readonly string outputPath;
        private readonly string outputExtension;
        private readonly List<(string sourceFolder, string filename, string extension)> files;

        public VideoMerger(List<(string sourceFolder, string filename, string extension)> fileViewModels, string outPath, string outExt)
        {
            Failed = false;
            Cancelled = false;
            outputPath = outPath;
            outputExtension = outExt;
            files = fileViewModels;
            GetMetadata(fileViewModels);
            foreach (var meta in metadataClasses)
                totalDuration += meta.format.duration;

            tempFile = Path.Combine(outputPath, $"temp_section_filenames{Guid.NewGuid()}.txt");
            using (var writeText = new StreamWriter(tempFile))
                for (var i = 0; i < fileViewModels.Count; i++)
                    writeText.WriteLine($"file '{fileViewModels[i].sourceFolder}\\{fileViewModels[i].filename}{fileViewModels[i].extension}'");
            SetList(new[] { "" });
        }

        public override void Setup() => DoSetup(null);

        protected override string CreateOutput(string obj, int index)
            => $"{outputPath}\\Merged_File{outputExtension}";

        protected override string CreateArguments(string obj, int index, ref string output)
        {
            var overwrite = false;
            if (File.Exists(output))
            {
                var args = new MessageEventArgs
                {
                    Message = $"The file {Path.GetFileName(output)} already exists. Overwrite? (Select \"No\" to output to a different file name.)"
                };
                ShowMessage(args);
                overwrite = args.Result;
            }
            var sb = new StringBuilder();
            if (overwrite)
                sb.Append("-y ");
            else
            {
                var filename = Path.GetFileNameWithoutExtension(output);
                output = $"{Path.GetDirectoryName(output)}\\{filename}[0]{Path.GetExtension(output)}";
            }
            var ext = files.First().extension;
            if (files.All(f => f.extension == ext))
                sb.Append($"-safe 0 -f concat -i \"{tempFile}\" -c copy \"{output}\"");
            else
            {
                foreach (var (folder, filename, extension) in files)
                    sb.Append($"-i \"{folder}\\{filename}{extension}\" ");
                sb.Append("-f lavfi -i anullsrc -filter_complex \"");
                for (int i = 0; i < files.Count; i++)
                    sb.Append(
                        $"[{i}:v]scale={metadataClasses.Max(m => m.streams[0].width)}:{metadataClasses.Max(m => m.streams[0].height)}:force_original_aspect_ratio=decrease,setsar=1," +
                        $"pad={metadataClasses.Max(m => m.streams[0].width)}:{metadataClasses.Max(m => m.streams[0].height)}:-1:-1:color=black[v{i}]; ");
                for (int i = 0; i < files.Count; i++)
                    sb.Append($"[v{i}][{i}:a]");
                sb.Append($"concat=n={files.Count}:v=1:a=1[outv][outa]\" -map \"[outv]\" -map \"[outa]\" {(outputExtension == ".mp4" ? "-vsync 2 " : string.Empty)}\"{output}\"");
            }

            return sb.ToString();
        }

        protected override TimeSpan? GetDuration(string obj) => totalDuration;

        public override void CancelOperation(string cancelMessage)
        {
            base.CancelOperation(cancelMessage);
            OnDownloadFinished(new FinishedEventArgs { Cancelled = Cancelled, Message = cancelMessage });
            CleanUp();
        }

        public void GetMetadata(List<(string folder, string name, string extension)> files)
        {
            for (int i = 0; i < files.Count; i++)
            {
                var info = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = Path.Combine(GetBinaryPath(), "ffprobe.exe"),
                    CreateNoWindow = true,
                    Arguments = $"-v quiet -print_format json -select_streams v:0 -show_entries stream=width,height -show_entries format=duration -sexagesimal \"{files[i].folder}\\{files[i].name}{files[i].extension}\""
                };
                var process = new Process { StartInfo = info };
                process.Start();
                process.WaitForExit();

                var result = process.StandardOutput;
                process.Dispose();
                using (var reader = new JsonTextReader(result))
                    metadataClasses.Add(new JsonSerializer().Deserialize<MetadataClass>(reader));
            }
        }

        protected override void CleanUp()
        {
            foreach (var stuff in ProcessStuff)
            {
                CloseProcess(stuff.Process, false);
                if (!Cancelled)
                    continue;

                if (!string.IsNullOrEmpty(stuff.Output))
                    File.Delete(stuff.Output);
            }
            Thread.Sleep(1000);
            if (!string.IsNullOrEmpty(tempFile))
                File.Delete(tempFile);
        }
    }
}