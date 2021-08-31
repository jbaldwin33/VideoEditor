using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using MVVMFramework;

namespace VideoUtilities
{
    public class VideoMerger : BaseClass<object>
    {
        private readonly string output;
        private readonly ProcessStartInfo startInfo;

        public event ProgressEventHandler ProgressDownload;
        public event FinishedDownloadEventHandler FinishedDownload;
        public event StartedDownloadEventHandler StartedDownload;
        public event ErrorEventHandler ErrorDownload;
        private Process process;
        private decimal percentage;
        private readonly List<MetadataClass> metadataClasses = new List<MetadataClass>();
        private TimeSpan totalDuration;
        private readonly string tempFile;

        public VideoMerger(List<(string sourceFolder, string filename, string extension)> fileViewModels, string outputPath, string outputExtension) : base(null)
        {
            Failed = false;
            Cancelled = false;

            GetMetadata(fileViewModels);
            foreach (var meta in metadataClasses)
                totalDuration += meta.format.duration;

            tempFile = Path.Combine(outputPath, $"temp_section_filenames{Guid.NewGuid()}.txt");
            using (var writeText = new StreamWriter(tempFile))
            {
                for (var i = 0; i < fileViewModels.Count; i++)
                    writeText.WriteLine($"file '{fileViewModels[i].sourceFolder}\\{fileViewModels[i].filename}{fileViewModels[i].extension}'");
            }

            output = $"{outputPath}\\Merged_File{outputExtension}";
            startInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = Path.Combine(GetBinaryPath(), "ffmpeg.exe"),
                CreateNoWindow = true
            };
            var sb = new StringBuilder("-y ");
            var ext = fileViewModels.First().extension;
            if (fileViewModels.All(f => f.extension == ext))
                sb.Append($"-safe 0 -f concat -i \"{tempFile}\" -c copy \"{output}\"");
            else
            {
                foreach (var (folder, filename, extension) in fileViewModels)
                    sb.Append($"-i \"{folder}\\{filename}{extension}\" ");
                sb.Append("-f lavfi -i anullsrc -filter_complex \"");
                for (int i = 0; i < fileViewModels.Count; i++)
                    sb.Append(
                        $"[{i}:v]scale={metadataClasses.Max(m => m.streams[0].width)}:{metadataClasses.Max(m => m.streams[0].height)}:force_original_aspect_ratio=decrease,setsar=1," +
                        $"pad={metadataClasses.Max(m => m.streams[0].width)}:{metadataClasses.Max(m => m.streams[0].height)}:-1:-1:color=black[v{i}]; ");
                for (int i = 0; i < fileViewModels.Count; i++)
                    sb.Append($"[v{i}][{i}:a]");
                sb.Append($"concat=n={fileViewModels.Count}:v=1:a=1[outv][outa]\" -map \"[outv]\" -map \"[outa]\" {(outputExtension == ".mp4" ? "-vsync 2 " : string.Empty)}\"{output}\"");
            }

            startInfo.Arguments = sb.ToString();
        }

        public void MergeVideo()
        {
            try
            {
                OnDownloadStarted(new DownloadStartedEventArgs { Label = Translatables.MergingLabel });
                process = new Process
                {
                    EnableRaisingEvents = true,
                    StartInfo = startInfo
                };
                process.Exited += Process_Exited;
                process.ErrorDataReceived += OutputHandler;
                process.Start();
                process.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                Failed = true;
                OnDownloadError(new ProgressEventArgs { Error = ex.Message });
            }
        }

        public override void CancelOperation(string cancelMessage)
        {
            base.CancelOperation(cancelMessage);
            OnDownloadFinished(new FinishedEventArgs { Cancelled = Cancelled, Message = cancelMessage });
            CleanUp();
        }

        private void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (Cancelled)
                return;

            var index = ProcessStuff.FindIndex(p => p.Process.Id == (sendingProcess as Process).Id);
            OnProgress(new ProgressEventArgs { Percentage = ProcessStuff[index].Finished ? 0 : percentage, Data = ProcessStuff[index].Finished ? string.Empty : outLine.Data });
            // extract the percentage from process output
            if (string.IsNullOrEmpty(outLine.Data) || ProcessStuff[index].Finished || isFinished())
                return;

            LastData = outLine.Data;
            if (outLine.Data.Contains("ERROR"))
            {
                Failed = true;
                OnDownloadError(new ProgressEventArgs { Error = outLine.Data });
                return;
            }

            if (!outLine.Data.Contains("Duration: ") && !isConverting())
                return;

            if (outLine.Data.Contains("Duration: "))
            {
                return;
            }

            var currentTime = TimeSpan.Zero;
            if (isConverting())
            {
                var strSub = outLine.Data.Split(new[] { "time=" }, StringSplitOptions.RemoveEmptyEntries)[1].Substring(0, 11);
                currentTime = TimeSpan.Parse(strSub);
            }

            // fire the process event
            var perc = Convert.ToDecimal((float)currentTime.TotalSeconds / (float)totalDuration.TotalSeconds) * 100;
            if (perc < 0)
            {
                Console.WriteLine("weird perc {0}", perc);
                return;
            }
            percentage = perc;
            OnProgress(new ProgressEventArgs { Percentage = perc, Data = outLine.Data });

            // is it finished?
            if (perc < 100 && !isFinished())
                return;

            if (perc >= 100 && !ProcessStuff[index].Finished)
                OnProgress(new ProgressEventArgs { Percentage = percentage, Data = outLine.Data });

            bool isConverting() => outLine.Data.Contains("frame=") && outLine.Data.Contains("fps=") && outLine.Data.Contains("time=");
            bool isFinished() => outLine.Data.Contains("global headers:") && outLine.Data.Contains("muxing overhead:");
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

        //protected override void Process_Exited(object sender, EventArgs e)
        //{
        //    CleanUp();
        //    if (ProcessStuff[index].Finished || Failed || Cancelled)
        //        return;

        //    if (process.ExitCode != 0 && !Cancelled)
        //    {
        //        OnDownloadError(new ProgressEventArgs { Error = LastData });
        //        return;
        //    }

        //    ProcessStuff[index].Finished = true;
        //    OnDownloadFinished(new FinishedEventArgs { Cancelled = Cancelled });
        //}

        private void CleanUp()
        {
            if (!process.HasExited)
                process.Close();

            Thread.Sleep(1000);
            if (!string.IsNullOrEmpty(tempFile))
                File.Delete(tempFile);
        }

        protected override void OnProgress(ProgressEventArgs e) => ProgressDownload?.Invoke(this, e);

        protected override void OnDownloadFinished(FinishedEventArgs e) => FinishedDownload?.Invoke(this, e);

        protected override void OnDownloadStarted(DownloadStartedEventArgs e) => StartedDownload?.Invoke(this, e);

        protected override void OnDownloadError(ProgressEventArgs e) => ErrorDownload?.Invoke(this, e);

        protected override void ErrorDataReceived(object sendingProcess, DataReceivedEventArgs error)
        {
            if (string.IsNullOrEmpty(error.Data))
                return;

            Failed = true;
            OnDownloadError(new ProgressEventArgs { Error = error.Data });
        }
    }
}