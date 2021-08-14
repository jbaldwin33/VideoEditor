using System;
using System.Diagnostics;

namespace VideoUtilities
{
    public abstract class BaseClass
    {
        public delegate void ProgressEventHandler(object sender, ProgressEventArgs e);
        public delegate void FinishedDownloadEventHandler(object sender, FinishedEventArgs e);
        public delegate void StartedDownloadEventHandler(object sender, DownloadStartedEventArgs e);
        public delegate void ErrorEventHandler(object sender, ProgressEventArgs e);

        public virtual void CancelOperation() => throw new NotImplementedException();
        protected virtual void OnDownloadFinished(FinishedEventArgs e) => throw new NotImplementedException();
        protected virtual void OnDownloadStarted(DownloadStartedEventArgs e) => throw new NotImplementedException();
        protected virtual void OnProgress(ProgressEventArgs e) => throw new NotImplementedException();
        protected virtual void OnDownloadError(ProgressEventArgs e) => throw new NotImplementedException();
        protected virtual void Process_Exited(object sender, EventArgs e) => throw new NotImplementedException();
        protected virtual void ErrorDataReceived(object sendingProcess, DataReceivedEventArgs error) => throw new NotImplementedException();
    }

    public class ProgressEventArgs : EventArgs
    {
        public decimal Percentage { get; set; }
        public string Data { get; set; }
        public string Error { get; set; }
    }

    public class DownloadStartedEventArgs : EventArgs
    {
        public string Label { get; set; }
    }

    public class FinishedEventArgs : EventArgs
    {
        public bool Cancelled { get; set; }
    }
}
