using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

namespace VideoUtilities
{
    public class VideoConverter : BaseClass
    {
        private readonly string sourceFolder;
        private readonly string sourceFileWithoutExtension;
        private readonly string inputExtension;
        private readonly string outputExtension;
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

        public VideoConverter(string folder, string fileWithoutExtension, string inExtension, string outExtension)
        {
            sourceFolder = folder;
            sourceFileWithoutExtension = fileWithoutExtension;
            inputExtension = inExtension;
            outputExtension = outExtension;

            var binaryPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Binaries");
            if (string.IsNullOrEmpty(binaryPath))
                throw new Exception("Cannot read 'binaryfolder' variable from app.config / web.config.");

            output = $"{sourceFolder}\\{sourceFileWithoutExtension}{outputExtension}";

            // setup the process that will fire youtube-dl
            startInfo = new ProcessStartInfo
            {
                Arguments = $"-i {sourceFolder}\\{sourceFileWithoutExtension}{inputExtension} {output}",
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
                OnDownloadStarted(new DownloadEventArgs());
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
                OnDownloadError(new ProgressEventArgs { Error = ex.Message });
            }
        }

        public override void CancelOperation()
        {
            if (!process.HasExited)
            {
                process.Kill();
                Thread.Sleep(100);
                File.Delete(output);
            }
        }

        private void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            OnProgress(new ProgressEventArgs { Percentage = finished ? 0 : percentage, Data = finished ? string.Empty : outLine.Data });
            // extract the percentage from process outpu
            if (string.IsNullOrEmpty(outLine.Data) || finished || isFinished())
            {
                OnDownloadFinished(new DownloadEventArgs());
                return;
            }

            if (outLine.Data.Contains("ERROR"))
            {
                OnDownloadError(new ProgressEventArgs() { Error = outLine.Data });
                return;
            }

            if (!outLine.Data.Contains("Duration: ") && !isConverting())
                return;

            if (outLine.Data.Contains("Duration: "))
            {
                duration = TimeSpan.Parse(outLine.Data.Split(new string[] { "Duration: " }, StringSplitOptions.None)[1].Substring(0, 11));
                return;
            }

            TimeSpan currentTime = TimeSpan.Zero;
            if (isConverting())
            {
                var strSub = outLine.Data.Split(new string[] { "time=" }, StringSplitOptions.RemoveEmptyEntries)[1].Substring(0, 11);
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
            OnProgress(new ProgressEventArgs() { Percentage = perc, Data = outLine.Data });

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

        }

        protected override void OnProgress(ProgressEventArgs e)
        {
            if (ProgressDownload != null)
                ProgressDownload(this, e);
        }

        protected override void OnDownloadFinished(DownloadEventArgs e)
        {
            if (!finished)
            {
                finished = true;
                FinishedDownload?.Invoke(this, e);
            }
        }

        protected override void OnDownloadStarted(DownloadEventArgs e) => StartedDownload?.Invoke(this, e);

        protected override void OnDownloadError(ProgressEventArgs e) => ErrorDownload?.Invoke(this, e);

        protected override void ErrorDataReceived(object sendingprocess, DataReceivedEventArgs error)
        {
            if (!string.IsNullOrEmpty(error.Data))
                OnDownloadError(new ProgressEventArgs() { Error = error.Data });
        }
    }
}
