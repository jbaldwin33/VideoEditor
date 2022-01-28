using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.VisualBasic.Devices;
using MVVMFramework.Localization;

namespace VideoUtilities
{
    public abstract class BaseClass
    {
        #region Events and fields

        public delegate void ProgressEventHandler(object sender, ProgressEventArgs e);
        public delegate void FinishedDownloadEventHandler(object sender, FinishedEventArgs e);
        public delegate void StartedDownloadEventHandler(object sender, DownloadStartedEventArgs e);
        public delegate void ErrorEventHandler(object sender, ProgressEventArgs e);
        public delegate void MessageEventHandler(object sender, MessageEventArgs e);
        public delegate void PreWorkFinishedEventHandler(object sender, PreWorkEventArgs e);
        public delegate void FirstWorkFinishedEventHandler(object sender, EventArgs e);
        public delegate void UpdatePlaylistEventHandler(object sender, PlaylistEventArgs e);
        public event ProgressEventHandler ProgressDownload;
        public event FinishedDownloadEventHandler FinishedDownload;
        public event StartedDownloadEventHandler StartedDownload;
        public event ErrorEventHandler ErrorDownload;
        public event MessageEventHandler MessageHandler;
        public event PreWorkFinishedEventHandler PreWorkFinished;
        public event FirstWorkFinishedEventHandler FirstWorkFinished;
        public event UpdatePlaylistEventHandler UpdatePlaylist;
        protected Action DoAfterProcessExit;
        protected bool Cancelled;
        protected bool Failed;
        protected bool ShowFile = true;
        protected List<string> ErrorData = new List<string>();
        protected readonly List<ProcessClass> CurrentProcess = new List<ProcessClass>();
        protected readonly List<ProcessClass> ProcessStuff = new List<ProcessClass>();
        protected int NumberFinished;
        protected int NumberInProcess;
        protected IEnumerable ObjectList;
        protected bool UseYoutubeDL;
        protected List<bool> IsList = new List<bool>();
        private readonly List<int> keepOutputList = new List<int>();
        private readonly object _lock = new object();
        private readonly object _lock2 = new object();
        private string binaryPath;
        protected string OutputPath;
        private string[] errorWords =
        {
            "ERROR",
            "Could not",
            "Invalid",
            "No such",
            "does not exist",
            "Failed to",
            "cannot",
            "Too many"
        };


        #endregion

        protected void SetList(IEnumerable list)
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
                        stuff.Process.BeginOutputReadLine();
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

        public virtual void PreWork() => throw new NotImplementedException();
        public virtual void SecondaryWork() => throw new NotImplementedException();

        public virtual void Setup() => throw new NotImplementedException();

        public void UpdateForPlaylist(int index, int current, int total, bool isPlaylist)
            => UpdatePlaylist?.Invoke(this, new PlaylistEventArgs { Index = index, Current = current, Total = total, IsPlaylist = isPlaylist });

        protected void DoSetup(Action callback)
        {
            DoAfterProcessExit = callback;
            var i = 0;
            foreach (var obj in ObjectList)
            {
                var output = CreateOutput(i, obj);
                var process = new Process
                {
                    EnableRaisingEvents = true,
                    StartInfo = new ProcessStartInfo
                    {
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        FileName = Path.Combine(GetBinaryPath(), UseYoutubeDL ? "youtube-dl.exe" : "ffmpeg.exe"),
                        CreateNoWindow = true,
                        Arguments = CreateArguments(i, ref output, obj)
                    }
                };
                process.Exited += Process_Exited;
                if (UseYoutubeDL)
                {
                    process.ErrorDataReceived += ErrorReceivedHandler;
                    process.OutputDataReceived += YoutubeOutputHandler;
                }
                else
                    process.ErrorDataReceived += OutputHandler;

                ProcessStuff.Add(new ProcessClass(false, process, output, TimeSpan.Zero, GetDuration(obj), new ProcessClass.YoutubeProps()));
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

        protected virtual string CreateArguments(int index, ref string output, object obj) => throw new NotImplementedException();
        protected virtual string CreateOutput(int index, object obj) => throw new NotImplementedException();
        protected virtual TimeSpan? GetDuration(object obj) => throw new NotImplementedException();

        public string GetBinaryPath() => !string.IsNullOrEmpty(binaryPath) ? binaryPath : binaryPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Binaries");

        protected virtual void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (Cancelled)
                return;

            if (string.IsNullOrEmpty(outLine.Data))
                ShowErrorData();

            var info = new ComputerInfo();
            var availableMemory = info.AvailablePhysicalMemory / (1024f * 1024f * 1024f);
            var totalMemory = info.TotalPhysicalMemory / (1024f * 1024f * 1024f);
            var usedMemoryPercentage = (totalMemory - availableMemory) / totalMemory * 100;

            if (usedMemoryPercentage > 95)
                CancelOperation(new RamUsageLabelTranslatable($"{usedMemoryPercentage:00}"));

            var index = ProcessStuff.FindIndex(p => p.Process.Id == (sendingProcess as Process).Id);
            OnProgress(new ProgressEventArgs { ProcessIndex = index, Percentage = ProcessStuff[index].Finished ? 0 : ProcessStuff[index].Percentage, Data = ProcessStuff[index].Finished ? string.Empty : outLine.Data });
            // extract the percentage from process output
            if (string.IsNullOrEmpty(outLine.Data) || ProcessStuff[index].Finished || IsFinished(outLine.Data))
                return;

            if (errorWords.Any(word => outLine.Data.Contains(word)))
            {
                Failed = true;
                ErrorData.Add(outLine.Data);
                return;
            }

            if (!outLine.Data.Contains("Duration: ") && !IsProcessing(outLine.Data))
                return;

            if (outLine.Data.Contains("Duration: "))
            {
                if (ProcessStuff[index].Duration == null)
                    ProcessStuff[index].Duration = TimeSpan.Parse(outLine.Data.Split(new[] { "Duration: " }, StringSplitOptions.None)[1].Substring(0, 11));

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

        public void YoutubeOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (Cancelled)
                return;

            var info = new ComputerInfo();
            var availableMemory = info.AvailablePhysicalMemory / (1024f * 1024f * 1024f);
            var totalMemory = info.TotalPhysicalMemory / (1024f * 1024f * 1024f);
            var usedMemoryPercentage = (totalMemory - availableMemory) / totalMemory * 100;

            if (usedMemoryPercentage > 95)
                CancelOperation(new RamUsageLabelTranslatable($"{usedMemoryPercentage:00}"));

            var index = ProcessStuff.FindIndex(p => p.Process.Id == (sendingProcess as Process).Id);
            //OnProgress(new ProgressEventArgs { ProcessIndex = index, Percentage = ProcessStuff[index].Finished ? 0 : youtubePercentage, Data = ProcessStuff[index].Finished ? string.Empty : outLine.Data });
            // extract the percentage from process output
            if (string.IsNullOrEmpty(outLine.Data) || ProcessStuff[index].Finished)
                return;

            if (outLine.Data.Contains("Finished downloading playlist"))
                ProcessStuff[index].YoutubeProperties.Downloaded++;

            if (ProcessStuff[index].YoutubeProperties.Downloaded > 0 &&
                ProcessStuff[index].YoutubeProperties.ToDownload > 0 &&
                ProcessStuff[index].YoutubeProperties.Downloaded == ProcessStuff[index].YoutubeProperties.ToDownload)
                return;

            if (errorWords.Any(word => outLine.Data.Contains(word)))
            {
                Failed = true;
                ErrorData.Add(outLine.Data);
                return;
            }

            if (!outLine.Data.Contains("[download]"))
                return;
            if (outLine.Data.Contains("Destination"))
            {
                var str = outLine.Data.Split(new[] { "Destination: " }, StringSplitOptions.None);
                ProcessStuff[index].Output = str[1];
            }

            if (outLine.Data.Contains("Downloading video ") && outLine.Data.Contains(" of "))
            {
                var str = outLine.Data.Substring(outLine.Data.IndexOf("video "));
                var b = string.Empty;
                var c = string.Empty;
                for (int i = 0; i < str.Length; i++)
                {
                    if (char.IsDigit(str[i]))
                        if (string.IsNullOrEmpty(b))
                            b += str[i];
                        else
                            c += str[i];
                }

                if (b.Length > 0)
                    ProcessStuff[index].YoutubeProperties.Downloaded = int.Parse(b) - 1;
                if (c.Length > 0)
                    ProcessStuff[index].YoutubeProperties.ToDownload = int.Parse(c);
                UpdateForPlaylist(index, ProcessStuff[index].YoutubeProperties.Downloaded + 1, ProcessStuff[index].YoutubeProperties.ToDownload, IsList[index]);
            }

            if (!PatternMatch(outLine.Data) && ((ProcessStuff[index].YoutubeProperties.Downloaded == 0 && ProcessStuff[index].YoutubeProperties.ToDownload == 0) || (ProcessStuff[index].YoutubeProperties.ToDownload != 0 && ProcessStuff[index].YoutubeProperties.Downloaded != ProcessStuff[index].YoutubeProperties.ToDownload)))
                return;

            // fire the process event
            var perc = IsList[index]
              ? GetPercentageForList(outLine.Data, index)
              : Convert.ToDecimal(Regex.Match(outLine.Data, @"\b\d+([\.,]\d+)?").Value, System.Globalization.CultureInfo.InvariantCulture);

            if (perc > 100 || perc < 0)
            {
                Console.WriteLine("weird perc {0}", perc);
                return;
            }
            OnProgress(new ProgressEventArgs { ProcessIndex = index, Percentage = perc, Data = outLine.Data });
        }

        private bool PatternMatch(string data)
        {
            var pattern = new Regex(@"\b\d+([\.,]\d+)?", RegexOptions.None);
            return data.Contains("Downloading playlist:")
                ? pattern.IsMatch(data.Substring(0, data.IndexOf(':')))
                : pattern.IsMatch(data);
        }

        protected bool CheckOverwrite(ref string output)
        {
            if (!File.Exists(output))
                return false;

            var args = new MessageEventArgs
            {
                Message = new FileAlreadyExistsTranslatable(Path.GetFileName(output))
            };
            ShowMessage(args);
            if (args.Result)
                return true;

            var filename = Path.GetFileNameWithoutExtension(output);
            output = $"{Path.GetDirectoryName(output)}\\{filename}[0]{Path.GetExtension(output)}";

            return args.Result;
        }

        protected void ErrorReceivedHandler(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data))
                return;
            Failed = true;
            CancelOperation(e.Data);
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
                ShowErrorData();
                return;
            }

            NumberFinished++;
            OnProgress(new ProgressEventArgs { ProcessIndex = index, Percentage = 100 });
            if (NumberFinished < ProcessStuff.Count)
                return;

            ProcessStuff[index].Finished = true;
            if (DoAfterProcessExit != null)
            {
                var action = DoAfterProcessExit;
                DoAfterProcessExit = null;
                action?.Invoke();
            }
            else
                OnDownloadFinished(new FinishedEventArgs { Cancelled = Cancelled });
        }

        private void ShowErrorData()
        {
            if (ErrorData.Count == 0)
                return;
            var sb = new StringBuilder();
            ErrorData.ForEach(error => sb.Append($"{error}\n"));
            OnDownloadError(new ProgressEventArgs { Error = sb.ToString() });
        }

        private decimal GetPercentageForList(string data, int index)
        {
            var val = Convert.ToDecimal(Regex.Match(data, @"\b\d+([\.,]\d+)?").Value, System.Globalization.CultureInfo.InvariantCulture);
            var downloaded = Convert.ToDecimal((float)ProcessStuff[index].YoutubeProperties.Downloaded);
            var toDownload = ProcessStuff[index].YoutubeProperties.ToDownload;
            return (val + (100 * downloaded)) / toDownload;
        }

        protected void CloseProcess(Process p, bool kill)
        {
            try
            {
                if (p.HasExited)
                    return;
                if (kill)
                    p.Kill();
                else
                    p.Close();
                Thread.Sleep(1000);
            }
            catch (InvalidOperationException)
            {

            }
        }

        protected static bool IsProcessing(string data) => data.Contains("frame=") && data.Contains("fps=") && data.Contains("time=");
        protected static bool IsFinished(string data) => data.Contains("global headers:") && data.Contains("muxing overhead:");


        public virtual void CancelOperation(string cancelMessage)
        {
            Cancelled = true;
            lock (_lock2)
            {
                foreach (var process in CurrentProcess)
                {
                    CloseProcess(process.Process, true);
                    if (!string.IsNullOrEmpty(process.Output))
                        File.Delete(process.Output);
                }
            }
            OnDownloadFinished(new FinishedEventArgs { Cancelled = Cancelled, Message = cancelMessage });
        }

        protected virtual void OnDownloadFinished(FinishedEventArgs e)
        {
            FinishedDownload?.Invoke(this, e);
            if (!e.Cancelled && ShowFile)
            {
                Process.Start("explorer.exe", $"/select,\"{OutputPath}\"");
                //open
            }
            CleanUp();
        }

        protected virtual void OnDownloadStarted(DownloadStartedEventArgs e) => StartedDownload?.Invoke(this, e);
        protected virtual void OnProgress(ProgressEventArgs e) => ProgressDownload?.Invoke(this, e);

        protected virtual void OnDownloadError(ProgressEventArgs e)
        {
            ErrorDownload?.Invoke(this, e);
            CleanUp();
        }

        protected virtual void OnPreWorkFinished(PreWorkEventArgs e) => PreWorkFinished?.Invoke(this, e);
        protected virtual void OnFirstWorkFinished(EventArgs e) => FirstWorkFinished?.Invoke(this, e);
        protected virtual void CleanUp() => throw new NotImplementedException();
        protected virtual void ShowMessage(MessageEventArgs e) => MessageHandler?.Invoke(this, e);
    }

    public class PreWorkEventArgs : EventArgs
    {
        public object Argument { get; set; }
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

    public class PlaylistEventArgs : EventArgs
    {
        public int Index { get; set; }
        public int Current { get; set; }
        public int Total { get; set; }
        public bool IsPlaylist { get; set; }
    }

    public class ProcessClass
    {
        public bool Finished { get; set; }
        public Process Process { get; set; }
        public string Output { get; set; }
        public TimeSpan CurrentTime { get; set; }
        public TimeSpan? Duration { get; set; }
        public YoutubeProps YoutubeProperties { get; set; }

        public decimal Percentage => Convert.ToDecimal((float)CurrentTime.TotalSeconds / (float)(Duration?.TotalSeconds ?? TimeSpan.MaxValue.TotalSeconds)) * 100;

        public ProcessClass(bool finished, Process process, string output, TimeSpan currentTime, TimeSpan? duration, YoutubeProps props = null)
        {
            Finished = finished;
            Process = process;
            Output = output;
            CurrentTime = currentTime;
            Duration = duration;
            YoutubeProperties = props;
        }

        public class YoutubeProps
        {
            public int ToDownload { get; set; }
            public int Downloaded { get; set; }
        }
    }
}
