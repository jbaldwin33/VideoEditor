using System;
using System.Collections.Generic;

namespace VideoUtilities
{
    public class VideoSizeReducer : BaseClass<(string Folder, string Filename, string Extension)>
    {
        private readonly string outputPath;
        public event ProgressEventHandler ProgressDownload;
        public event FinishedDownloadEventHandler FinishedDownload;
        public event StartedDownloadEventHandler StartedDownload;
        public event ErrorEventHandler ErrorDownload;

        public VideoSizeReducer(IEnumerable<(string Folder, string Filename, string Extension)> fileViewModels, string outPath)
        {
            Failed = false;
            Cancelled = false;
            outputPath = outPath;
            SetList(fileViewModels);
            DoSetup(null);
        }

        protected override string CreateOutput((string Folder, string Filename, string Extension) obj, int index) => $"{outputPath}\\{obj.Filename}_reduced{obj.Extension}";
        protected override string CreateArguments((string Folder, string Filename, string Extension) obj, int index, string output) 
            => $"-y -i \"{obj.Folder}\\{obj.Filename}{obj.Extension}\" -vcodec libx264 -crf 28 \"{output}\"";

        protected override TimeSpan? GetDuration((string Folder, string Filename, string Extension) obj) => null;

        public override void CancelOperation(string cancelMessage)
        {
            base.CancelOperation(cancelMessage);
            OnDownloadFinished(new FinishedEventArgs { Cancelled = Cancelled, Message = cancelMessage });
        }

        protected override void CleanUp()
        {
            
        }

        protected override void OnProgress(ProgressEventArgs e) => ProgressDownload?.Invoke(this, e);

        protected override void OnDownloadFinished(FinishedEventArgs e) => FinishedDownload?.Invoke(this, e);

        protected override void OnDownloadStarted(DownloadStartedEventArgs e) => StartedDownload?.Invoke(this, e);

        protected override void OnDownloadError(ProgressEventArgs e) => ErrorDownload?.Invoke(this, e);
    }
}
