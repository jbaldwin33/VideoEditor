using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using MVVMFramework.Localization;

namespace VideoUtilities
{
    public class VideoMerger : BaseClass
    {
        private readonly List<MetadataClass> metadataClasses = new List<MetadataClass>();
        private readonly string tempFile;
        private readonly string outputExtension;
        private readonly List<(string sourceFolder, string filename, string extension)> files;
        private TimeSpan totalDuration;

        public VideoMerger(List<(string sourceFolder, string filename, string extension)> fileViewModels, string outPath, string outExt)
        {
            Failed = false;
            Cancelled = false;
            OutputPath = $"{outPath}\\Merged_File{outExt}";
            outputExtension = outExt;
            files = fileViewModels;
            tempFile = Path.Combine(outPath, $"temp_section_filenames{Guid.NewGuid()}.txt");

            SetList(new[] { "" });
        }

        public override void Setup() => DoSetup(null);
        public override void PreWork()
        {
            GetMetadata(files);
            foreach (var meta in metadataClasses)
            {
                if (meta.format == null)
                {
                    CancelOperation("Error getting metadata for video files. Please make sure the video files are not corrupted.");//todo
                    return;
                }
                totalDuration += meta.format.duration;
            }

            using (var writeText = new StreamWriter(tempFile))
                for (var i = 0; i < files.Count; i++)
                    writeText.WriteLine($"file '{files[i].sourceFolder}\\{files[i].filename}{files[i].extension}'");
            Setup();
            DoWork(new MergingLabelTranslatable());
        }

        protected override string CreateOutput(int index, object obj)
            => $"{Path.GetDirectoryName(OutputPath)}\\Merged_File{outputExtension}";

        protected override string CreateArguments(int index, ref string output, object obj)
        {
            var sb = new StringBuilder($"{(CheckOverwrite(ref output) ? "-y" : string.Empty)}");
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

        protected override TimeSpan? GetDuration(object obj) => totalDuration;

        public void GetMetadata(List<(string folder, string name, string extension)> files)
        {
            var jsonSerializer = new JsonSerializer();
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
                    metadataClasses.Add(jsonSerializer.Deserialize<MetadataClass>(reader));
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