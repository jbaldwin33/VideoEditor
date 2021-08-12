using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoUtilities
{
    public abstract class BaseClass
    {
        public delegate void ProgressEventHandler(object sender, ProgressEventArgs e);
        public delegate void FinishedDownloadEventHandler(object sender, DownloadEventArgs e);
        public delegate void StartedDownloadEventHandler(object sender, DownloadEventArgs e);
        public delegate void ErrorEventHandler(object sender, ProgressEventArgs e);

        public virtual void CancelOperation() => throw new NotImplementedException();
        protected virtual void OnDownloadFinished(DownloadEventArgs e) => throw new NotImplementedException();
        protected virtual void OnDownloadStarted(DownloadEventArgs e) => throw new NotImplementedException();
        protected virtual void OnProgress(ProgressEventArgs e) => throw new NotImplementedException();
        protected virtual void OnDownloadError(ProgressEventArgs e) => throw new NotImplementedException();
        protected virtual void Process_Exited(object sender, EventArgs e) => throw new NotImplementedException();
        protected virtual void ErrorDataReceived(object sendingprocess, DataReceivedEventArgs error) => throw new NotImplementedException();
    }

    public class ProgressEventArgs : EventArgs
    {
        public decimal Percentage { get; set; }
        public string Data { get; set; }
        public string Error { get; set; }

        public ProgressEventArgs() : base()
        {

        }

    }

    public class DownloadEventArgs : EventArgs
    {
        public DownloadEventArgs() : base()
        {

        }

    }
}
