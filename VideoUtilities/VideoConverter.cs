using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

namespace VideoUtilities
{
    public class VideoConverter : BaseClass
    {
        private readonly string output;
        private readonly ProcessStartInfo startInfo;

        public event ProgressEventHandler ProgressDownload;
        public event FinishedDownloadEventHandler FinishedDownload;
        public event StartedDownloadEventHandler StartedDownload;
        public event ErrorEventHandler ErrorDownload;
        private Process process;
        private bool finished;
        private decimal percentage;
        private TimeSpan duration;
        private bool cancelled;
        private bool failed;
        private string lastData;

        public VideoConverter(string folder, string fileWithoutExtension, string inExtension, string outExtension)
        {
            failed = false;
            cancelled = false;
            var binaryPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Binaries");
            if (string.IsNullOrEmpty(binaryPath))
                throw new Exception("Cannot read 'binaryFolder' variable from app.config / web.config.");

            output = $"{folder}\\{fileWithoutExtension}{outExtension}";

            // setup the process that will fire youtube-dl
            startInfo = new ProcessStartInfo
            {
                Arguments = $"-i {folder}\\{fileWithoutExtension}{inExtension} {output}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = Path.Combine(binaryPath, "ffmpeg.exe"),
                CreateNoWindow = true
            };
        }

        public void ConvertVideo()
        {
            try
            {
                OnDownloadStarted(new DownloadStartedEventArgs { Label = "Converting video..." });
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
        }

        public override void CancelOperation()
        {
            cancelled = true;
            if (!process.HasExited)
            {
                process.Kill();
                Thread.Sleep(1000);
            }
            File.Delete(output);
            OnDownloadFinished(new FinishedEventArgs { Cancelled = cancelled });
        }

        private void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (cancelled)
                return;

            OnProgress(new ProgressEventArgs { Percentage = finished ? 0 : percentage, Data = finished ? string.Empty : outLine.Data });
            // extract the percentage from process output
            if (string.IsNullOrEmpty(outLine.Data) || finished || isFinished())
                return;

            lastData = outLine.Data;
            if (outLine.Data.Contains("ERROR"))
            {
                failed = true;
                OnDownloadError(new ProgressEventArgs { Error = outLine.Data });
                return;
            }

            if (!outLine.Data.Contains("Duration: ") && !isConverting())
                return;

            if (outLine.Data.Contains("Duration: "))
            {
                duration = TimeSpan.Parse(outLine.Data.Split(new[] { "Duration: " }, StringSplitOptions.None)[1].Substring(0, 11));
                return;
            }

            var currentTime = TimeSpan.Zero;
            if (isConverting())
            {
                var strSub = outLine.Data.Split(new[] { "time=" }, StringSplitOptions.RemoveEmptyEntries)[1].Substring(0, 11);
                currentTime = TimeSpan.Parse(strSub);
            }

            // fire the process event
            var perc = Convert.ToDecimal((float)currentTime.TotalSeconds / (float)duration.TotalSeconds) * 100;
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

            if (perc >= 100 && !finished)
                OnProgress(new ProgressEventArgs { Percentage = percentage, Data = outLine.Data });

            bool isConverting() => outLine.Data.Contains("frame=") && outLine.Data.Contains("fps=") && outLine.Data.Contains("time=");
            bool isFinished() => outLine.Data.Contains("global headers:") && outLine.Data.Contains("muxing overhead:");
        }

        protected override void Process_Exited(object sender, EventArgs e)
        {
            if (finished || failed || cancelled)
                return;

            if (process.ExitCode != 0 && !cancelled)
            {
                OnDownloadError(new ProgressEventArgs { Error = lastData });
                return;
            }

            finished = true;
            OnDownloadFinished(new FinishedEventArgs { Cancelled = cancelled });
        }

        protected override void OnProgress(ProgressEventArgs e) => ProgressDownload?.Invoke(this, e);

        protected override void OnDownloadFinished(FinishedEventArgs e) => FinishedDownload?.Invoke(this, e);

        protected override void OnDownloadStarted(DownloadStartedEventArgs e) => StartedDownload?.Invoke(this, e);

        protected override void OnDownloadError(ProgressEventArgs e) => ErrorDownload?.Invoke(this, e);

        protected override void ErrorDataReceived(object sendingProcess, DataReceivedEventArgs error)
        {
            if (string.IsNullOrEmpty(error.Data))
                return;

            failed = true;
            OnDownloadError(new ProgressEventArgs { Error = error.Data });
        }
    }
}
