using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using MVVMFramework;

namespace VideoUtilities
{
    public class VideoConverter : BaseClass
    {
        public event ProgressEventHandler ProgressDownload;
        public event FinishedDownloadEventHandler FinishedDownload;
        public event StartedDownloadEventHandler StartedDownload;
        public event ErrorEventHandler ErrorDownload;

        private bool finished;
        private bool cancelled;
        private bool failed;
        private string lastData;
        private readonly List<ProcessClass> processStuff = new List<ProcessClass>();
        private readonly List<ProcessClass> currentProcess = new List<ProcessClass>();
        private int numberInProcess;
        private int numberFinished;

        public VideoConverter(IEnumerable<(string folder, string filename, string extension)> fileViewModels, string outExtension)
        {
            failed = false;
            cancelled = false;
            var binaryPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Binaries");
            if (string.IsNullOrEmpty(binaryPath))
                throw new Exception("Cannot read 'binaryFolder' variable from app.config / web.config.");


            foreach (var (folder, filename, extension) in fileViewModels)
            {
                var output = $"{folder}\\{filename}_converted{outExtension}";
                var process = new Process
                {
                    EnableRaisingEvents = true,
                    StartInfo = new ProcessStartInfo
                    {
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        FileName = Path.Combine(binaryPath, "ffmpeg.exe"),
                        CreateNoWindow = true,
                        Arguments = $"-y -i \"{folder}\\{filename}{extension}\" -c:a copy -c:v copy \"{output}\""
                    }
                };
                process.Exited += Process_Exited;
                process.ErrorDataReceived += OutputHandler;
                processStuff.Add(new ProcessClass(process, output, TimeSpan.Zero, TimeSpan.MaxValue));
            }
        }

        public void ConvertVideo()
        {
            foreach (var stuff in processStuff)
            {
                currentProcess.Add(stuff);
                try
                {
                    OnDownloadStarted(new DownloadStartedEventArgs { Label = Translatables.ConvertingLabel });
                    numberInProcess++;
                    stuff.Process.Start();
                    stuff.Process.BeginErrorReadLine();
                    while (numberInProcess >= 2)
                    {
                        Thread.Sleep(200);
                    }
                }
                catch (Exception ex)
                {
                    failed = true;
                    OnDownloadError(new ProgressEventArgs { Error = ex.Message });
                }
            }
        }

        public override void CancelOperation(string cancelMessage)
        {
            cancelled = true;
            for (var i = 0; i < currentProcess.Count; i++)
            {
                if (!currentProcess[i].Process.HasExited)
                {
                    currentProcess[i].Process.Kill();
                    Thread.Sleep(1000);
                }

                if (!string.IsNullOrEmpty(currentProcess[i].Output))
                    File.Delete(currentProcess[i].Output);
            }
            OnDownloadFinished(new FinishedEventArgs { Cancelled = cancelled, Message = cancelMessage});
        }

        private void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (cancelled)
                return;
            var index = processStuff.FindIndex(p => p.Process.Id == (sendingProcess as Process).Id);
            OnProgress(new ProgressEventArgs { ProcessIndex = index, Percentage = finished ? 0 : processStuff[index].Percentage, Data = finished ? string.Empty : outLine.Data });
            // extract the percentage from process output
            if (string.IsNullOrEmpty(outLine.Data) || finished || isFinished())
                return;

            lastData = outLine.Data;
            if (outLine.Data.Contains("ERROR"))
            {
                failed = true;
                OnDownloadError(new ProgressEventArgs { Error = outLine.Data });
                return;
            }

            if (!outLine.Data.Contains("Duration: ") && !isConverting())
                return;

            if (outLine.Data.Contains("Duration: "))
            {
                processStuff[index].Duration = TimeSpan.Parse(outLine.Data.Split(new[] { "Duration: " }, StringSplitOptions.None)[1].Substring(0, 11));
                return;
            }

            if (isConverting())
            {
                var strSub = outLine.Data.Split(new[] { "time=" }, StringSplitOptions.RemoveEmptyEntries)[1].Substring(0, 11);
                processStuff[index].CurrentTime = TimeSpan.Parse(strSub);
            }

            OnProgress(new ProgressEventArgs { ProcessIndex = index, Percentage = processStuff[index].Percentage, Data = outLine.Data });
            if (processStuff[index].Percentage < 100 && !isFinished())
                return;

            if (processStuff[index].Percentage >= 100 && !finished)
                OnProgress(new ProgressEventArgs { ProcessIndex = index, Percentage = processStuff[index].Percentage, Data = outLine.Data });

            bool isConverting() => outLine.Data.Contains("frame=") && outLine.Data.Contains("fps=") && outLine.Data.Contains("time=");
            bool isFinished() => outLine.Data.Contains("global headers:") && outLine.Data.Contains("muxing overhead:");
        }

        protected override void Process_Exited(object sender, EventArgs e)
        {
            if (finished || failed || cancelled)
                return;
            var processClass = currentProcess.First(p => p.Process.Id == (sender as Process).Id);
            var index = processStuff.FindIndex(p => p.Process.Id == processClass.Process.Id);
            currentProcess.Remove(processClass);
            numberInProcess--;

            if (processClass.Process.ExitCode != 0 && !cancelled)
            {
                OnDownloadError(new ProgressEventArgs { Error = lastData });
                return;
            }

            numberFinished++;
            OnProgress(new ProgressEventArgs { ProcessIndex = index, Percentage = 100 });
            if (numberFinished < processStuff.Count)
                return;

            finished = true;
            OnDownloadFinished(new FinishedEventArgs { Cancelled = cancelled });
        }

        protected override void OnProgress(ProgressEventArgs e) => ProgressDownload?.Invoke(this, e);

        protected override void OnDownloadFinished(FinishedEventArgs e) => FinishedDownload?.Invoke(this, e);

        protected override void OnDownloadStarted(DownloadStartedEventArgs e) => StartedDownload?.Invoke(this, e);

        protected override void OnDownloadError(ProgressEventArgs e) => ErrorDownload?.Invoke(this, e);

        protected override void ErrorDataReceived(object sendingProcess, DataReceivedEventArgs error)
        {
            if (string.IsNullOrEmpty(error.Data))
                return;

            failed = true;
            OnDownloadError(new ProgressEventArgs { Error = error.Data });
        }
    }
}
