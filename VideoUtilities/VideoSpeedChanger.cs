using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using MVVMFramework;

namespace VideoUtilities
{
    public class VideoSpeedChanger : BaseClass<object>
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

        public VideoSpeedChanger(string fullPath, double speed, Enums.ScaleRotate scaleRotate) : base(null)
        {
            Failed = false;
            Cancelled = false;

            output = $"{Path.GetDirectoryName(fullPath)}\\{Path.GetFileNameWithoutExtension(fullPath)}_formatted{Path.GetExtension(fullPath)}";
            startInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = Path.Combine(GetBinaryPath(), "ffmpeg.exe"),
                CreateNoWindow = true
            };

            var filter = string.Empty;
            switch (scaleRotate)
            {
                case Enums.ScaleRotate.NoSNoR: break;
                case Enums.ScaleRotate.NoS90R: filter = ",transpose=1"; break;
                case Enums.ScaleRotate.NoS180R: filter = ",vflip,hflip"; break;
                case Enums.ScaleRotate.NoS270R: filter = ",transpose=2"; break;
                case Enums.ScaleRotate.SNoR: filter = ",hflip"; break;
                case Enums.ScaleRotate.S90R: filter = ",hflip,transpose=1"; break;
                case Enums.ScaleRotate.S180R: filter = ",vflip"; break;
                case Enums.ScaleRotate.S270R: filter = ",hflip,transpose=2"; break;
                default: throw new ArgumentOutOfRangeException(nameof(scaleRotate), scaleRotate, null);
            }

            startInfo.Arguments = $"-y -i \"{fullPath}\" -filter_complex \"[0:v]setpts={1 / speed}*PTS{filter}[v];[0:a]atempo={speed}[a]\" -map \"[v]\" -map \"[a]\" \"{output}\"";
        }

        public void ChangeSpeed()
        {
            try
            {
                OnDownloadStarted(new DownloadStartedEventArgs { Label = Translatables.ChangingLabel });
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
        }

        private void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (Cancelled)
                return;

            OnProgress(new ProgressEventArgs { Percentage = finished ? 0 : percentage, Data = finished ? string.Empty : outLine.Data });
            // extract the percentage from process output
            if (string.IsNullOrEmpty(outLine.Data) || finished || isFinished())
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

        //protected override void Process_Exited(object sender, EventArgs e)
        //{
        //    if (finished || Failed || Cancelled)
        //        return;

        //    if (process.ExitCode != 0 && !Cancelled)
        //    {
        //        OnDownloadError(new ProgressEventArgs { Error = LastData });
        //        return;
        //    }

        //    finished = true;
        //    OnDownloadFinished(new FinishedEventArgs { Cancelled = Cancelled });
        //}

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