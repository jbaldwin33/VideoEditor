using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace VideoUtilities
{
    public abstract class BaseClass
    {
        public delegate void ProgressEventHandler(object sender, ProgressEventArgs e);
        public delegate void FinishedDownloadEventHandler(object sender, FinishedEventArgs e);
        public delegate void StartedDownloadEventHandler(object sender, DownloadStartedEventArgs e);
        public delegate void ErrorEventHandler(object sender, ProgressEventArgs e);
        public delegate void MessageEventHandler(object sender, MessageEventArgs e);

        private string path;
        public string GetBinaryPath() => !string.IsNullOrEmpty(path) ? path : path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Binaries");

        public virtual void CancelOperation(string cancelMessage) => throw new NotImplementedException();
        protected virtual void OnDownloadFinished(FinishedEventArgs e) => throw new NotImplementedException();
        protected virtual void OnDownloadStarted(DownloadStartedEventArgs e) => throw new NotImplementedException();
        protected virtual void OnProgress(ProgressEventArgs e) => throw new NotImplementedException();
        protected virtual void OnDownloadError(ProgressEventArgs e) => throw new NotImplementedException();
        protected virtual void OnShowMessage(MessageEventArgs e) => throw new NotImplementedException();
        protected virtual void Process_Exited(object sender, EventArgs e) => throw new NotImplementedException();
        protected virtual void ErrorDataReceived(object sendingProcess, DataReceivedEventArgs error) => throw new NotImplementedException();
    }

    public class MessageEventArgs : EventArgs
    {
        public string Message { get; set; }
        public bool Result { get; set; }
    }

    public class ProgressEventArgs : EventArgs
    {
        public int ProcessIndex { get; set; }
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
        public int ProcessIndex { get; set; }
        public bool Cancelled { get; set; }
        public string Message { get; set; }
    }
}
