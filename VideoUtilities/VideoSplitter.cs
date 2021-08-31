using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using MVVMFramework;

namespace VideoUtilities
{
    public class VideoSplitter : BaseClass
    {
        public event ProgressEventHandler ProgressDownload;
        public event FinishedDownloadEventHandler FinishedDownload;
        public event StartedDownloadEventHandler StartedDownload;
        public event ErrorEventHandler ErrorDownload;
        public event EventHandler SplitFinished;

        private bool splitFinished;
        private readonly string sourceFolder;
        private readonly string sourceFileWithoutExtension;
        private readonly string extension;
        private readonly bool outputDifferentFormat;
        private readonly string outputFormat;
        private readonly bool combineVideo;
        private string tempFile;
        private bool cancelled;
        private string lastData;
        private bool failed;
        private bool combineFinished;
        private readonly List<ProcessClass> currentProcess = new List<ProcessClass>();
        private readonly List<ProcessClass> processStuff = new List<ProcessClass>();
        private int numberFinished;
        private int numberInProcess;

        public VideoSplitter(string fullInputPath, IReadOnlyList<Tuple<TimeSpan, TimeSpan, string>> times, bool combine, bool outputDiffFormat, string outFormat, bool reEncodeVideo)
        {
            cancelled = false;
            splitFinished = false;
            sourceFolder = Path.GetDirectoryName(fullInputPath);
            sourceFileWithoutExtension = Path.GetFileNameWithoutExtension(fullInputPath);
            extension = Path.GetExtension(fullInputPath);
            combineVideo = combine;
            outputDifferentFormat = outputDiffFormat;
            outputFormat = outFormat;
            var i = 0;
            foreach (var (startTime, endTime, _) in times)
            {
                var output = $"{sourceFolder}\\{sourceFileWithoutExtension}_trimmed{i + 1}{(outputDifferentFormat ? outputFormat : extension)}";
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
                        Arguments = $"-y -i \"{fullInputPath}\" {(reEncodeVideo ? string.Empty : "-codec copy")} -ss {startTime.TotalSeconds} -to {endTime.TotalSeconds} \"{output}\""
                    }
                };
                process.Exited += Process_Exited;
                process.ErrorDataReceived += OutputHandler;
                processStuff.Add(new ProcessClass(process, output, TimeSpan.Zero, endTime - startTime));
                i++;
            }
        }

        public void Split()
        {
            try
            {
                foreach (var stuff in processStuff)
                {
                    currentProcess.Add(stuff);

                    OnDownloadStarted(new DownloadStartedEventArgs { Label = Translatables.SplittingLabel });
                    numberInProcess++;
                    stuff.Process.Start();
                    stuff.Process.BeginErrorReadLine();
                    while (numberInProcess >= 2) { Thread.Sleep(200); }
                }
            }
            catch (Exception ex)
            {
                failed = true;
                OnDownloadError(new ProgressEventArgs { Error = ex.Message });
            }
        }

        public void CombineSections()
        {
            try
            {
                OnDownloadStarted(new DownloadStartedEventArgs { Label = Translatables.CombiningSectionsLabel });
                tempFile = Path.Combine(sourceFolder, $"temp_section_filenames{Guid.NewGuid()}.txt");
                File.WriteAllLines(tempFile, processStuff.Select(x => $"file '{x.Output}'"));
                var combinedFile = $"{sourceFolder}\\{sourceFileWithoutExtension}_combined{(outputDifferentFormat ? outputFormat : extension)}";
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
                        Arguments = $"-safe 0 -f concat -i \"{tempFile}\" -c copy \"{combinedFile}\""
                    }
                };
                process.Exited += CombineProcess_Exited;
                process.ErrorDataReceived += CombineProcessOutputHandler;
                var duration = processStuff.Aggregate(TimeSpan.Zero, (current, processClass) => current + processClass.Duration);
                var stuff = new ProcessClass(process, combinedFile, TimeSpan.Zero, duration);
                processStuff.Add(stuff);
                currentProcess.Add(stuff);
                process.Start();
                process.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                failed = true;
                OnDownloadError(new ProgressEventArgs { Error = ex.Message });
            }
        }


        public override void CancelOperation(string cancelMessage)
        {
            try
            {
                cancelled = true;
                foreach (var stuff in currentProcess)
                {
                    if (!stuff.Process.HasExited)
                    {
                        stuff.Process.Kill();
                        Thread.Sleep(1000);
                    }

                    if (!string.IsNullOrEmpty(stuff.Output))
                        File.Delete(stuff.Output);
                }
                //delete all or just those in process?
                //processStuff.ForEach(p => File.Delete(p.Output));
                if (!string.IsNullOrEmpty(tempFile))
                    File.Delete(tempFile);

                OnDownloadFinished(new FinishedEventArgs { Cancelled = cancelled, Message = cancelMessage });
            }
            catch (Exception ex)
            {
                failed = true;
                OnDownloadError(new ProgressEventArgs { Error = ex.Message });
            }
        }

        private void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (cancelled)
                return;

            var index = processStuff.FindIndex(p => p.Process.Id == (sendingProcess as Process).Id);
            OnProgress(new ProgressEventArgs { ProcessIndex = index, Percentage = splitFinished ? 100 : processStuff[index].Percentage, Data = splitFinished ? string.Empty : outLine.Data });
            if (string.IsNullOrEmpty(outLine.Data) || splitFinished || IsFinished(outLine.Data))
                return;

            lastData = outLine.Data;
            if (outLine.Data.Contains("ERROR") || outLine.Data.Contains("Invalid"))
            {
                failed = true;
                OnDownloadError(new ProgressEventArgs { Error = outLine.Data });
                return;
            }

            if (!IsProcessing(outLine.Data))
                return;

            if (IsProcessing(outLine.Data))
            {
                var strSub = outLine.Data.Split(new[] { "time=" }, StringSplitOptions.RemoveEmptyEntries)[1].Substring(0, 11);
                processStuff[index].CurrentTime = TimeSpan.Parse(strSub);
            }

            OnProgress(new ProgressEventArgs { ProcessIndex = index, Percentage = processStuff[index].Percentage, Data = outLine.Data });

            // is it finished?
            if (processStuff[index].Percentage < 100 && !IsFinished(outLine.Data))
                return;

            if (processStuff[index].Percentage >= 100 && !splitFinished)
                OnProgress(new ProgressEventArgs { ProcessIndex = index, Percentage = processStuff[index].Percentage, Data = outLine.Data });
        }

        private void CombineProcessOutputHandler(object sender, DataReceivedEventArgs outLine)
        {
            if (cancelled)
                return;

            var index = processStuff.FindIndex(p => p.Process.Id == (sender as Process).Id);
            OnProgress(new ProgressEventArgs { Percentage = processStuff[index].Percentage, Data = outLine.Data });
            if (string.IsNullOrEmpty(outLine.Data) || combineFinished || IsFinished(outLine.Data))
                return;

            lastData = outLine.Data;
            if (outLine.Data.Contains("ERROR") || outLine.Data.Contains("Invalid"))
            {
                failed = true;
                OnDownloadError(new ProgressEventArgs { Error = outLine.Data });
                return;
            }

            if (!IsProcessing(outLine.Data))
                return;

            if (IsProcessing(outLine.Data))
            {
                var strSub = outLine.Data.Split(new[] { "time=" }, StringSplitOptions.RemoveEmptyEntries)[1].Substring(0, 11);
                processStuff[index].CurrentTime = TimeSpan.Parse(strSub);
            }

            OnProgress(new ProgressEventArgs { Percentage = processStuff[index].Percentage, Data = outLine.Data });

            // is it finished?
            if (processStuff[index].Percentage < 100 && !IsFinished(outLine.Data))
                return;

            if (processStuff[index].Percentage >= 100 && !combineFinished)
                OnProgress(new ProgressEventArgs { Percentage = processStuff[index].Percentage, Data = outLine.Data });
        }

        protected override void Process_Exited(object sender, EventArgs e)
        {
            if (failed || cancelled)
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

            splitFinished = true;
            if (combineVideo)
                OnSplitFinished(EventArgs.Empty);
            else
            {
                FinishedDownload?.Invoke(this, new FinishedEventArgs { Cancelled = cancelled });
                CleanUp();
            }
        }

        private void CombineProcess_Exited(object sender, EventArgs e)
        {
            if (failed || cancelled)
                return;

            var processClass = processStuff.First(p => p.Process.Id == (sender as Process).Id);
            currentProcess.Remove(processClass);
            processStuff.Remove(processClass); //so that we don't delete the merged output in CleanUp
            if (processClass.Process.ExitCode != 0 && !cancelled)
            {
                OnDownloadError(new ProgressEventArgs { Error = lastData });
                return;
            }
            OnProgress(new ProgressEventArgs { Percentage = 100 });
            combineFinished = true;
            FinishedDownload?.Invoke(this, new FinishedEventArgs { Cancelled = cancelled });
            CleanUp();
        }

        private void CleanUp()
        {
            foreach (var stuff in processStuff)
            {
                if (!stuff.Process.HasExited)
                {
                    stuff.Process.Close();
                    Thread.Sleep(1000);
                }

                if (!combineVideo && !cancelled)
                    continue;

                if (!string.IsNullOrEmpty(stuff.Output))
                    File.Delete(stuff.Output);
            }
            Thread.Sleep(1000);
            if (!string.IsNullOrEmpty(tempFile))
                File.Delete(tempFile);
        }

        private static bool IsProcessing(string data) => data.Contains("frame=") && data.Contains("fps=") && data.Contains("time=");
        private static bool IsFinished(string data) => data.Contains("global headers:") && data.Contains("muxing overhead:");

        protected override void OnProgress(ProgressEventArgs e) => ProgressDownload?.Invoke(this, e);

        protected override void OnDownloadFinished(FinishedEventArgs e) => FinishedDownload?.Invoke(this, e);

        protected override void OnDownloadStarted(DownloadStartedEventArgs e) => StartedDownload?.Invoke(this, e);

        protected override void OnDownloadError(ProgressEventArgs e)
        {
            ErrorDownload?.Invoke(this, e);
            CleanUp();
        }

        protected override void ErrorDataReceived(object sendingProcess, DataReceivedEventArgs error)
        {
            if (string.IsNullOrEmpty(error.Data))
                return;

            failed = true;
            OnDownloadError(new ProgressEventArgs { Error = error.Data });
        }

        public void OnSplitFinished(EventArgs e) => SplitFinished?.Invoke(this, e);
    }
}
