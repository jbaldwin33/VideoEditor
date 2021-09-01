using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.VisualBasic.Devices;
using MVVMFramework;

namespace VideoUtilities
{
    public abstract class BaseClass<T>
    {
        public delegate void ProgressEventHandler(object sender, ProgressEventArgs e);
        public delegate void FinishedDownloadEventHandler(object sender, FinishedEventArgs e);
        public delegate void StartedDownloadEventHandler(object sender, DownloadStartedEventArgs e);
        public delegate void ErrorEventHandler(object sender, ProgressEventArgs e);
        public delegate void MessageEventHandler(object sender, MessageEventArgs e);
        protected Action DoAfterExit { get; set; }
        protected bool Cancelled;
        protected string LastData;
        protected bool Failed;
        protected readonly List<ProcessClass> CurrentProcess = new List<ProcessClass>();
        protected readonly List<ProcessClass> ProcessStuff = new List<ProcessClass>();
        protected int NumberFinished;
        protected int NumberInProcess;
        protected IEnumerable<T> ObjectList;
        private string path;
        private List<int> keepOutputList = new List<int>();
        private object _lock = new object();

        protected void SetList(IEnumerable<T> list)
        {
            ObjectList = list;
        }

        public void DoWork(string label)
        {
            try
            {
                lock (_lock)
                {
                    foreach (var stuff in ProcessStuff)
                    {
                        CurrentProcess.Add(stuff);
                        OnDownloadStarted(new DownloadStartedEventArgs { Label = label });
                        NumberInProcess++;
                        stuff.Process.Start();
                        stuff.Process.BeginErrorReadLine();
                        while (NumberInProcess >= 2)
                        {
                            Thread.Sleep(200);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Failed = true;
                OnDownloadError(new ProgressEventArgs { Error = ex.Message });
            }
        }

        protected void DoSetup(Action callback)
        {
            DoAfterExit = callback;
            var i = 0;
            foreach (var obj in ObjectList)
            {
                var output = CreateOutput(obj, i);
                var process = new Process
                {
                    EnableRaisingEvents = true,
                    StartInfo = new ProcessStartInfo
                    {
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        FileName = Path.Combine(GetBinaryPath(), "ffmpeg.exe"),
                        CreateNoWindow = true,
                        Arguments = CreateArguments(obj, i, output)
                    }
                };
                process.Exited += Process_Exited;
                process.ErrorDataReceived += OutputHandler;
                ProcessStuff.Add(new ProcessClass(false, process, output, TimeSpan.Zero, GetDuration(obj)));
                i++;
            }
        }

        protected void AddProcess(string args, string output, TimeSpan? duration, bool keepOutput)
        {
            var process = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = Path.Combine(GetBinaryPath(), "ffmpeg.exe"),
                    CreateNoWindow = true,
                    Arguments = args
                }
            };
            process.Exited += Process_Exited;
            process.ErrorDataReceived += OutputHandler;
            var stuff = new ProcessClass(false, process, output, TimeSpan.Zero, duration);
            lock (_lock) { ProcessStuff.Insert(0, stuff); }
            if (keepOutput)
                keepOutputList.Add(ProcessStuff.IndexOf(stuff));
            CurrentProcess.Add(stuff);
            process.Start();
            process.BeginErrorReadLine();
        }

        protected virtual string CreateArguments(T obj, int index, string output) => throw new NotImplementedException();
        protected virtual string CreateOutput(T obj, int index) => throw new NotImplementedException();
        protected virtual TimeSpan? GetDuration(T obj) => throw new NotImplementedException();

        public string GetBinaryPath() => !string.IsNullOrEmpty(path) ? path : path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Binaries");

        protected void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (Cancelled)
                return;

            var info = new ComputerInfo();
            var availableMemory = info.AvailablePhysicalMemory / (1024f * 1024f * 1024f);
            var totalMemory = info.TotalPhysicalMemory / (1024f * 1024f * 1024f);
            var usedMemoryPercentage = (totalMemory - availableMemory) / totalMemory * 100;

            if (usedMemoryPercentage > 95)
                CancelOperation(string.Format(Translatables.RamUsageLabel, $"{usedMemoryPercentage:00}"));

            var index = ProcessStuff.FindIndex(p => p.Process.Id == (sendingProcess as Process).Id);
            OnProgress(new ProgressEventArgs { ProcessIndex = index, Percentage = ProcessStuff[index].Finished ? 0 : ProcessStuff[index].Percentage, Data = ProcessStuff[index].Finished ? string.Empty : outLine.Data });
            // extract the percentage from process output
            if (string.IsNullOrEmpty(outLine.Data) || ProcessStuff[index].Finished || IsFinished(outLine.Data))
                return;

            LastData = outLine.Data;
            if (outLine.Data.Contains("ERROR"))
            {
                Failed = true;
                OnDownloadError(new ProgressEventArgs { Error = outLine.Data });
                return;
            }

            if (!outLine.Data.Contains("Duration: ") && !IsProcessing(outLine.Data))
                return;

            if (outLine.Data.Contains("Duration: "))
            {
                if (ProcessStuff[index].Duration == null)
                {
                    ProcessStuff[index].Duration = TimeSpan.Parse(outLine.Data.Split(new[] { "Duration: " }, StringSplitOptions.None)[1].Substring(0, 11));
                    //if (ProcessStuff[index].Duration > TimeSpan.FromMinutes(1))
                    //{
                    //    var args = new MessageEventArgs { Message = Translatables.VideoTooBigMessage };
                    //    OnShowMessage(args);
                    //    if (!args.Result)
                    //        CancelOperation(string.Empty);
                    //}
                }

                return;
            }

            if (IsProcessing(outLine.Data))
            {
                var strSub = outLine.Data.Split(new[] { "time=" }, StringSplitOptions.RemoveEmptyEntries)[1].Substring(0, 11);
                ProcessStuff[index].CurrentTime = TimeSpan.Parse(strSub);
            }

            OnProgress(new ProgressEventArgs { ProcessIndex = index, Percentage = ProcessStuff[index].Percentage, Data = outLine.Data });
            if (ProcessStuff[index].Percentage < 100 && !IsProcessing(outLine.Data))
                return;

            if (ProcessStuff[index].Percentage >= 100 && !ProcessStuff[index].Finished)
                OnProgress(new ProgressEventArgs { ProcessIndex = index, Percentage = ProcessStuff[index].Percentage, Data = outLine.Data });
        }

        protected void Process_Exited(object sender, EventArgs e)
        {
            var processClass = CurrentProcess.First(p => p.Process.Id == (sender as Process).Id);
            var index = ProcessStuff.FindIndex(p => p.Process.Id == processClass.Process.Id);
            if (Failed || Cancelled)
                return;

            CurrentProcess.Remove(processClass);
            NumberInProcess--;
            foreach (var toKeep in keepOutputList)
                ProcessStuff[toKeep].Output = string.Empty;

            if (processClass.Process.ExitCode != 0 && !Cancelled)
            {
                OnDownloadError(new ProgressEventArgs { Error = LastData });
                return;
            }

            NumberFinished++;
            OnProgress(new ProgressEventArgs { ProcessIndex = index, Percentage = 100 });
            if (NumberFinished < ProcessStuff.Count)
                return;

            ProcessStuff[index].Finished = true;
            if (DoAfterExit != null)
            {
                var action = DoAfterExit;
                DoAfterExit = null;
                action?.Invoke();
            }
            else
                OnDownloadFinished(new FinishedEventArgs { Cancelled = Cancelled });
        }

        protected static bool IsProcessing(string data) => data.Contains("frame=") && data.Contains("fps=") && data.Contains("time=");
        protected static bool IsFinished(string data) => data.Contains("global headers:") && data.Contains("muxing overhead:");


        public virtual void CancelOperation(string cancelMessage)
        {
            Cancelled = true;
            lock (_lock)
            {
                foreach (var process in CurrentProcess)
                {
                    if (!process.Process.HasExited)
                    {
                        process.Process.Kill();
                        Thread.Sleep(1000);
                    }

                    if (!string.IsNullOrEmpty(process.Output))
                        File.Delete(process.Output);
                }
            }
        }

        protected virtual void OnDownloadFinished(FinishedEventArgs e) => CleanUp();

        protected virtual void OnDownloadStarted(DownloadStartedEventArgs e) => throw new NotImplementedException();
        protected virtual void OnProgress(ProgressEventArgs e) => throw new NotImplementedException();

        protected virtual void OnDownloadError(ProgressEventArgs e) => CleanUp();

        protected virtual void CleanUp() => throw new NotImplementedException();
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

    public class ProcessClass
    {
        public bool Finished { get; set; }
        public Process Process { get; set; }
        public string Output { get; set; }
        public TimeSpan CurrentTime { get; set; }
        public TimeSpan? Duration { get; set; }

        public decimal Percentage => Convert.ToDecimal((float)CurrentTime.TotalSeconds / (float)(Duration?.TotalSeconds ?? TimeSpan.MaxValue.TotalSeconds)) * 100;

        public ProcessClass(bool finished, Process process, string output, TimeSpan currentTime, TimeSpan? duration)
        {
            Finished = finished;
            Process = process;
            Output = output;
            CurrentTime = currentTime;
            Duration = duration;
        }
    }
}
