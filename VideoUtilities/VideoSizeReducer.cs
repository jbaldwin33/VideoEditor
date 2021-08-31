using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using MVVMFramework;

namespace VideoUtilities
{
    public class VideoSizeReducer : BaseClass<(string Folder, string Filename, string Extension)>
    {
        private readonly string outputPath;
        public event ProgressEventHandler ProgressDownload;
        public event FinishedDownloadEventHandler FinishedDownload;
        public event StartedDownloadEventHandler StartedDownload;
        public event ErrorEventHandler ErrorDownload;

        public VideoSizeReducer(IEnumerable<(string Folder, string Filename, string Extension)> fileViewModels, string outPath) : base(fileViewModels)
        {
            Failed = false;
            Cancelled = false;
            outputPath = outPath;
            DoSetup(null);
            //foreach (var (folder, filename, extension) in fileViewModels)
            //{
            //    var output = $"{outPath}\\{filename}_reduced{extension}";
            //    var process = new Process
            //    {
            //        EnableRaisingEvents = true,
            //        StartInfo = new ProcessStartInfo
            //        {
            //            UseShellExecute = false,
            //            RedirectStandardOutput = true,
            //            RedirectStandardError = true,
            //            WindowStyle = ProcessWindowStyle.Hidden,
            //            FileName = Path.Combine(GetBinaryPath(), "ffmpeg.exe"),
            //            CreateNoWindow = true,
            //            Arguments = $"-y -i \"{folder}\\{filename}{extension}\" -vcodec libx264 -crf 28 \"{output}\""
            //        }
            //    };
            //    process.Exited += Process_Exited;
            //    process.ErrorDataReceived += OutputHandler;
            //    ProcessStuff.Add(new ProcessClass(false, process, output, TimeSpan.Zero, null));
            //}

        }

        protected override string CreateOutput((string Folder, string Filename, string Extension) obj, int index) => $"{outputPath}\\{obj.Filename}_reduced{obj.Extension}";
        protected override string CreateArguments((string Folder, string Filename, string Extension) obj, int index, string output) 
            => $"-y -i \"{obj.Folder}\\{obj.Filename}{obj.Extension}\" -vcodec libx264 -crf 28 \"{output}\"";

        protected override TimeSpan? GetDuration((string Folder, string Filename, string Extension) obj) => null;

        public void ReduceSize()
        {
            foreach (var stuff in ProcessStuff)
            {
                CurrentProcess.Add(stuff);
                try
                {
                    OnDownloadStarted(new DownloadStartedEventArgs { Label = Translatables.ReducingSizeLabel });
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

            OnDownloadFinished(new FinishedEventArgs { Cancelled = Cancelled, Message = cancelMessage });
        }

        //private void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        //{
        //    if (Cancelled)
        //        return;
        //    var index = ProcessStuff.FindIndex(p => p.Process.Id == (sendingProcess as Process).Id);
        //    OnProgress(new ProgressEventArgs { ProcessIndex = index, Percentage = ProcessStuff[index].Finished ? 100 : ProcessStuff[index].Percentage, Data = ProcessStuff[index].Finished ? string.Empty : outLine.Data });
        //    // extract the percentage from process output
        //    if (string.IsNullOrEmpty(outLine.Data) || ProcessStuff[index].Finished || isFinished())
        //        return;

        //    LastData = outLine.Data;
        //    if (outLine.Data.Contains("ERROR"))
        //    {
        //        Failed = true;
        //        OnDownloadError(new ProgressEventArgs { Error = outLine.Data });
        //        return;
        //    }

        //    if (!outLine.Data.Contains("Duration: ") && !isConverting())
        //        return;

        //    if (outLine.Data.Contains("Duration: "))
        //    {
        //        ProcessStuff[index].Duration = TimeSpan.Parse(outLine.Data.Split(new[] { "Duration: " }, StringSplitOptions.None)[1].Substring(0, 11));
        //        return;
        //    }

        //    if (isConverting())
        //    {
        //        var strSub = outLine.Data.Split(new[] { "time=" }, StringSplitOptions.RemoveEmptyEntries)[1].Substring(0, 11);
        //        ProcessStuff[index].CurrentTime = TimeSpan.Parse(strSub);
        //    }

        //    OnProgress(new ProgressEventArgs { ProcessIndex = index, Percentage = ProcessStuff[index].Percentage, Data = outLine.Data });
        //    if (ProcessStuff[index].Percentage < 100 && !isFinished())
        //        return;

        //    if (ProcessStuff[index].Percentage >= 100 && !ProcessStuff[index].Finished)
        //        OnProgress(new ProgressEventArgs { ProcessIndex = index, Percentage = ProcessStuff[index].Percentage, Data = outLine.Data });

        //    bool isConverting() => outLine.Data.Contains("frame=") && outLine.Data.Contains("fps=") && outLine.Data.Contains("time=");
        //    bool isFinished() => outLine.Data.Contains("global headers:") && outLine.Data.Contains("muxing overhead:");
        //}

        protected override void CleanUp()
        {
            
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
