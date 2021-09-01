using System;
using System.Diagnostics;
using System.IO;

namespace VideoUtilities
{
    public class VideoSpeedChanger : BaseClass<string>
    {
        public event ProgressEventHandler ProgressDownload;
        public event FinishedDownloadEventHandler FinishedDownload;
        public event StartedDownloadEventHandler StartedDownload;
        public event ErrorEventHandler ErrorDownload;
        private readonly Enums.ScaleRotate scaleRotate;
        private readonly double newSpeed;

        public VideoSpeedChanger(string fullPath, double speed, Enums.ScaleRotate sr) : base(new[] { fullPath })
        {
            Failed = false;
            Cancelled = false;
            scaleRotate = sr;
            newSpeed = speed;
            DoSetup(null);
        }

        protected override string CreateOutput(string obj, int index)
            => $"{Path.GetDirectoryName(obj)}\\{Path.GetFileNameWithoutExtension(obj)}_formatted{Path.GetExtension(obj)}";

        protected override string CreateArguments(string obj, int index, string output)
        {
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

            return $"-y -i \"{obj}\" -filter_complex \"[0:v]setpts={1 / newSpeed}*PTS{filter}[v];[0:a]atempo={newSpeed}[a]\" -map \"[v]\" -map \"[a]\" \"{output}\"";
        }

        protected override TimeSpan? GetDuration(string obj) => null;
        
        public override void CancelOperation(string cancelMessage)
        {
            base.CancelOperation(cancelMessage);
            OnDownloadFinished(new FinishedEventArgs { Cancelled = Cancelled, Message = cancelMessage });
        }

        protected override void OnProgress(ProgressEventArgs e) => ProgressDownload?.Invoke(this, e);

        protected override void OnDownloadFinished(FinishedEventArgs e) => FinishedDownload?.Invoke(this, e);

        protected override void OnDownloadStarted(DownloadStartedEventArgs e) => StartedDownload?.Invoke(this, e);

        protected override void OnDownloadError(ProgressEventArgs e) => ErrorDownload?.Invoke(this, e);
    }
}