using System;
using System.Collections.Generic;

namespace VideoUtilities
{
    public class VideoConverter : BaseClass<(string Folder, string Filename, string Extension)>
    {
        private readonly string outExtension;
        public event ProgressEventHandler ProgressDownload;
        public event FinishedDownloadEventHandler FinishedDownload;
        public event StartedDownloadEventHandler StartedDownload;
        public event ErrorEventHandler ErrorDownload;

        public VideoConverter(IEnumerable<(string folder, string filename, string extension)> fileViewModels, string outExt) : base(fileViewModels)
        {
            Failed = false;
            Cancelled = false;
            outExtension = outExt;
            DoSetup(null);
        }

        protected override string CreateOutput((string Folder, string Filename, string Extension) obj, int index) 
            => $"{obj.Folder}\\{obj.Filename}_converted{outExtension}";

        protected override string CreateArguments((string Folder, string Filename, string Extension) obj, int index, string output) 
            => $"-y -i \"{obj.Folder}\\{obj.Filename}{obj.Extension}\" -c:a copy -c:v copy \"{output}\"";

        protected override TimeSpan? GetDuration((string Folder, string Filename, string Extension) obj) => null;
        
        public override void CancelOperation(string cancelMessage)
        {
            base.CancelOperation(cancelMessage);
            OnDownloadFinished(new FinishedEventArgs { Cancelled = Cancelled, Message = cancelMessage});
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
