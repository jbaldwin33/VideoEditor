using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace VideoUtilities
{
    public class VideoConverter : BaseClass
    {
        private string sourceFolder;
        private string sourceFileWithoutExtension;
        private string inputExtension;
        private string outputExtension;
        private ProcessStartInfo startInfo;

        public event ProgressEventHandler ProgressDownload;
        public event FinishedDownloadEventHandler FinishedDownload;
        public event StartedDownloadEventHandler StartedDownload;
        public event ErrorEventHandler ErrorDownload;

        public VideoConverter(string folder, string fileWithoutExtension, string inExtension, string outExtension)
        {
            sourceFolder = folder;
            sourceFileWithoutExtension = fileWithoutExtension;
            inputExtension = inExtension;
            outputExtension = outExtension;

            var binaryPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Binaries");
            if (string.IsNullOrEmpty(binaryPath))
                throw new Exception("Cannot read 'binaryfolder' variable from app.config / web.config.");

            var output = $"{sourceFolder}\\{sourceFileWithoutExtension}{outputExtension}";
            startInfo.Arguments = $"-i {sourceFolder}\\{sourceFileWithoutExtension}{inputExtension} {output}";

            // setup the process that will fire youtube-dl
            startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.FileName = Path.Combine(binaryPath, "ffmpeg.exe");
            startInfo.CreateNoWindow = true;
        }

        public void Download()
        {
            try
            {
                OnDownloadStarted(new DownloadEventArgs());
                var process = new Process();
                process.EnableRaisingEvents = true;
                process.Exited += Process_Exited;
                process.ErrorDataReceived += new DataReceivedEventHandler(ErrorDataReceived);
                process.StartInfo = startInfo;
                process.Start();
                var perc = 0;//Convert.ToDecimal((float)(i + 1) / (float)arguments.Count * 100);
                OnProgress(new ProgressEventArgs { Percentage = perc });

                OnDownloadFinished(new DownloadEventArgs());
            }
            catch (Exception ex)
            {
                OnDownloadError(new ProgressEventArgs { Error = ex.Message });
            }
        }


        protected virtual void OnProgress(ProgressEventArgs e)
        {
            if (ProgressDownload != null)
                ProgressDownload(this, e);
        }

        protected override void OnDownloadFinished(DownloadEventArgs e)
        {
            FinishedDownload?.Invoke(this, e);
            CleanUp();
        }

        protected override void OnDownloadStarted(DownloadEventArgs e) => StartedDownload?.Invoke(this, e);

        protected override void OnDownloadError(ProgressEventArgs e)
        {
            ErrorDownload?.Invoke(this, e);
            CleanUp();
        }

        protected override void Process_Exited(object sender, EventArgs e) => OnDownloadFinished(new DownloadEventArgs());


        protected override void ErrorDataReceived(object sendingprocess, DataReceivedEventArgs error)
        {
            if (!string.IsNullOrEmpty(error.Data))
                OnDownloadError(new ProgressEventArgs() { Error = error.Data });
        }

        private void CleanUp()
        {
            throw new NotImplementedException();
        }
    }
}
