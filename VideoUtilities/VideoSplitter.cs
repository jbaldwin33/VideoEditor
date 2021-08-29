using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using MVVMFramework;

namespace VideoUtilities
{
    public class VideoSplitter : BaseClass
    {
        public event ProgressEventHandler ProgressDownload;
        public event FinishedDownloadEventHandler FinishedDownload;
        public event StartedDownloadEventHandler StartedDownload;
        public event ErrorEventHandler ErrorDownload;

        private bool splitFinished;
        private readonly string sourceFolder;
        private readonly string sourceFileWithoutExtension;
        private readonly string extension;
        private readonly bool outputDifferentFormat;
        private readonly string outputFormat;
        private readonly List<Tuple<TimeSpan, TimeSpan, string>> times;
        private readonly bool combineVideo;
        private readonly ProcessStartInfo startInfo;
        private readonly List<string> filenamesWithExtra = new List<string>();
        private readonly List<string> filenames = new List<string>();
        private string tempfile;
        private decimal percentage;
        private bool cancelled;
        private TimeSpan newDur = TimeSpan.Zero;
        private Process process;
        private string lastData;
        private bool failed;
        private bool combineFinished;
        private string combinedFile;

        public VideoSplitter(string fullInputPath, List<Tuple<TimeSpan, TimeSpan, string>> t, bool combine, bool outputDiffFormat, string outFormat, bool reEncodeVideo)
        {
            cancelled = false;
            splitFinished = false;
            sourceFolder = Path.GetDirectoryName(fullInputPath);
            sourceFileWithoutExtension = Path.GetFileName(fullInputPath);
            extension = Path.GetExtension(fullInputPath);
            times = t;
            combineVideo = combine;
            outputDifferentFormat = outputDiffFormat;
            outputFormat = outFormat;

            var binaryPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Binaries");
            if (string.IsNullOrEmpty(binaryPath))
                throw new Exception("Cannot read 'binaryFolder' variable from app.config / web.config.");

            var args = $"-y -i \"{fullInputPath}\"";
            var sb = new StringBuilder(args);

            for (int i = 0; i < times.Count; i++)
            {
                var output = $"{sourceFolder}\\{sourceFileWithoutExtension}_trimmed{i + 1}{(outputDifferentFormat ? outputFormat : extension)}";
                sb.Append($"{(reEncodeVideo ? string.Empty : " -codec copy")} -ss {times[i].Item1.TotalSeconds} -to {times[i].Item2.TotalSeconds} \"{output}\"");

                filenames.Add(output);
                filenamesWithExtra.Add($"file '{output}'");

                newDur += times[i].Item2 - times[i].Item1;
            }
            // setup the process that will fire youtube-dl
            startInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = Path.Combine(binaryPath, "ffmpeg.exe"),
                CreateNoWindow = true,
                Arguments = sb.ToString()
            };
        }

        public void Split()
        {
            try
            {
                OnDownloadStarted(new DownloadStartedEventArgs { Label = Translatables.SplittingLabel });
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
                failed = true;
                OnDownloadError(new ProgressEventArgs { Error = ex.Message });
            }

            while (splitFinished == false || combineFinished == false)
            {
                Thread.Sleep(100); // wait while process exits;
            }
        }

        public override void CancelOperation(string cancelMessage)
        {
            cancelled = true;
            if (!process.HasExited)
                process.Kill();

            Thread.Sleep(1000);
            if (!string.IsNullOrEmpty(tempfile))
                File.Delete(tempfile);
            if (!string.IsNullOrEmpty(combinedFile))
                File.Delete(combinedFile);
            filenames.ForEach(File.Delete);
            OnDownloadFinished(new FinishedEventArgs { Cancelled = cancelled, Message = cancelMessage});
        }

        private void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (cancelled)
                return;

            OnProgress(new ProgressEventArgs { Percentage = splitFinished ? 0 : percentage, Data = splitFinished ? string.Empty : outLine.Data });
            if (string.IsNullOrEmpty(outLine.Data) || splitFinished || IsFinished(outLine.Data))
                return;

            lastData = outLine.Data;
            if (outLine.Data.Contains("ERROR") || outLine.Data.Contains("Invalid"))
            {
                failed = true;
                OnDownloadError(new ProgressEventArgs { Error = outLine.Data });
                return;
            }

            if (!IsProcessing(outLine.Data))
                return;

            var currentTime = TimeSpan.Zero;
            if (IsProcessing(outLine.Data))
            {
                var strSub = outLine.Data.Split(new[] { "time=" }, StringSplitOptions.RemoveEmptyEntries)[1].Substring(0, 11);
                currentTime = TimeSpan.Parse(strSub);
            }

            // fire the process event
            var perc = Convert.ToDecimal((float)currentTime.TotalSeconds / (float)newDur.TotalSeconds) * 100;
            if (perc < 0)
            {
                Console.WriteLine($"weird percentage {perc}");
                return;
            }
            percentage = perc;
            OnProgress(new ProgressEventArgs { Percentage = perc, Data = outLine.Data });

            // is it finished?
            if (perc < 100 && !IsFinished(outLine.Data))
                return;

            if (perc >= 100 && !splitFinished)
                OnProgress(new ProgressEventArgs { Percentage = percentage, Data = outLine.Data });
        }

        private void CleanUp()
        {
            if (!process.HasExited)
                process.Close();
            
            Thread.Sleep(1000);
            if (!string.IsNullOrEmpty(tempfile))
                File.Delete(tempfile);
            if (combineVideo || cancelled)
            {
                filenames.ForEach(File.Delete);
                if (!string.IsNullOrEmpty(combinedFile))
                    File.Delete(combinedFile);
            }
        }

        private void CombineSections()
        {
            OnDownloadStarted(new DownloadStartedEventArgs { Label = Translatables.CombiningSectionsLabel });
            tempfile = Path.Combine(sourceFolder, $"temp_section_filenames{Guid.NewGuid()}.txt");
            File.WriteAllLines(tempfile, filenamesWithExtra);
            combinedFile = $"{sourceFolder}\\{sourceFileWithoutExtension}_combined{(outputDifferentFormat ? outputFormat : extension)}";
            startInfo.Arguments = $"-safe 0 -f concat -i \"{tempfile}\" -c copy \"{combinedFile}\"";
            process = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = startInfo
            };
            process.Exited += CombineProcess_Exited;
            process.ErrorDataReceived += CombineProcess_ErrorDataReceived;

            process.Start();
            process.BeginErrorReadLine();
        }

        private void CombineProcess_ErrorDataReceived(object sender, DataReceivedEventArgs outLine)
        {
            if (cancelled)
                return;

            if (string.IsNullOrEmpty(outLine.Data) || combineFinished || IsFinished(outLine.Data))
            {
                OnProgress(new ProgressEventArgs { Percentage = 100, Data = outLine.Data });
                return;
            }
            
            lastData = outLine.Data;
            if (!outLine.Data.Contains("ERROR") && !outLine.Data.Contains("Invalid")) 
                return;

            failed = true;
            OnDownloadError(new ProgressEventArgs { Error = outLine.Data });
        }

        private static bool IsProcessing(string data) => data.Contains("frame=") && data.Contains("fps=") && data.Contains("time=");
        private static bool IsFinished(string data) => data.Contains("global headers:") && data.Contains("muxing overhead:");

        private void CombineProcess_Exited(object sender, EventArgs e)
        {
            if (process.ExitCode != 0 || failed || cancelled)
            {
                if (process.ExitCode != 0 && !cancelled)
                    OnDownloadError(new ProgressEventArgs { Error = lastData });
                CleanUp();
                return;
            }

            combineFinished = true;
            FinishedDownload?.Invoke(this, new FinishedEventArgs { Cancelled = cancelled });
            CleanUp();
        }

        protected override void Process_Exited(object sender, EventArgs e)
        {
            if (process.ExitCode != 0 || failed || cancelled)
            {
                if (process.ExitCode != 0 && !cancelled)
                    OnDownloadError(new ProgressEventArgs { Error = lastData });
                CleanUp();
                return;
            }

            splitFinished = true;
            if (combineVideo)
                CombineSections();
            else
            {
                FinishedDownload?.Invoke(this, new FinishedEventArgs { Cancelled = cancelled });
                CleanUp();
            }
        }

        protected override void OnProgress(ProgressEventArgs e) => ProgressDownload?.Invoke(this, e);

        protected override void OnDownloadFinished(FinishedEventArgs e) => FinishedDownload?.Invoke(this, e);

        protected override void OnDownloadStarted(DownloadStartedEventArgs e) => StartedDownload?.Invoke(this, e);

        protected override void OnDownloadError(ProgressEventArgs e)
        {
            ErrorDownload?.Invoke(this, e);
            CleanUp();
        }

        protected override void ErrorDataReceived(object sendingProcess, DataReceivedEventArgs error)
        {
            if (string.IsNullOrEmpty(error.Data))
                return;

            failed = true;
            OnDownloadError(new ProgressEventArgs { Error = error.Data });
        }
    }
}
