using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using MVVMFramework;

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

        public void ConvertVideo()
        {
            foreach (var stuff in ProcessStuff)
            {
                CurrentProcess.Add(stuff);
                try
                {
                    OnDownloadStarted(new DownloadStartedEventArgs { Label = Translatables.ConvertingLabel });
                    NumberInProcess++;
                    stuff.Process.Start();
                    stuff.Process.BeginErrorReadLine();
                    while (NumberInProcess >= 2)
                    {
                        Thread.Sleep(200);
                    }
                }
                catch (Exception ex)
                {
                    Failed = true;
                    OnDownloadError(new ProgressEventArgs { Error = ex.Message });
                }
            }
        }

        public override void CancelOperation(string cancelMessage)
        {
            base.CancelOperation(cancelMessage);
            OnDownloadFinished(new FinishedEventArgs { Cancelled = Cancelled, Message = cancelMessage});
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
