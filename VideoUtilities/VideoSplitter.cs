using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using MVVMFramework;

namespace VideoUtilities
{
    public class VideoSplitter : BaseClass<(TimeSpan StartTime, TimeSpan EndTime, string Title)>
    {
        public event ProgressEventHandler ProgressDownload;
        public event FinishedDownloadEventHandler FinishedDownload;
        public event StartedDownloadEventHandler StartedDownload;
        public event ErrorEventHandler ErrorDownload;
        public event EventHandler SplitFinished;

        private readonly string sourceFolder;
        private readonly string sourceFileWithoutExtension;
        private readonly string extension;
        private readonly bool outputDifferentFormat;
        private readonly string outputFormat;
        private readonly bool combineVideo;
        private string tempFile;
        private string fullInputPath;
        private readonly bool doReEncode;

        public VideoSplitter(string fullPath, List<(TimeSpan, TimeSpan, string)> times, bool combine, bool outputDiffFormat, string outFormat, bool reEncodeVideo) : base(times)
        {
            Cancelled = false;
            fullInputPath = fullPath;
            doReEncode = reEncodeVideo;
            sourceFolder = Path.GetDirectoryName(fullPath);
            sourceFileWithoutExtension = Path.GetFileNameWithoutExtension(fullPath);
            extension = Path.GetExtension(fullPath);
            combineVideo = combine;
            outputDifferentFormat = outputDiffFormat;
            outputFormat = outFormat;
            DoSetup(() => OnSplitFinished(EventArgs.Empty));
        }

        protected override string CreateOutput((TimeSpan, TimeSpan, string) obj, int index) => $"{sourceFolder}\\{sourceFileWithoutExtension}_trimmed{index + 1}{(outputDifferentFormat ? outputFormat : extension)}";
        protected override string CreateArguments((TimeSpan StartTime, TimeSpan EndTime, string Title) obj, int index, string output) =>
            $"-y -i \"{fullInputPath}\" {(doReEncode ? string.Empty : "-codec copy")} -ss {obj.StartTime.TotalSeconds} -to {obj.EndTime.TotalSeconds} \"{output}\"";

        protected override TimeSpan? GetDuration((TimeSpan StartTime, TimeSpan EndTime, string Title) obj) => obj.EndTime- obj.StartTime;

        //public void Split()
        //{
        //    try
        //    {
        //        foreach (var stuff in ProcessStuff)
        //        {
        //            CurrentProcess.Add(stuff);

        //            OnDownloadStarted(new DownloadStartedEventArgs { Label = Translatables.SplittingLabel });
        //            NumberInProcess++;
        //            stuff.Process.Start();
        //            stuff.Process.BeginErrorReadLine();
        //            while (NumberInProcess >= 2) { Thread.Sleep(200); }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Failed = true;
        //        OnDownloadError(new ProgressEventArgs { Error = ex.Message });
        //    }
        //}

        public void CombineSections()
        {
            try
            {
                OnDownloadStarted(new DownloadStartedEventArgs { Label = Translatables.CombiningSectionsLabel });
                tempFile = Path.Combine(sourceFolder, $"temp_section_filenames{Guid.NewGuid()}.txt");
                File.WriteAllLines(tempFile, ProcessStuff.Select(x => $"file '{x.Output}'"));
                var combinedFile = $"{sourceFolder}\\{sourceFileWithoutExtension}_combined{(outputDifferentFormat ? outputFormat : extension)}";
                var args = $"-safe 0 -f concat -i \"{tempFile}\" -c copy \"{combinedFile}\"";
                AddProcess(args, combinedFile, true);
            }
            catch (Exception ex)
            {
                Failed = true;
                OnDownloadError(new ProgressEventArgs { Error = ex.Message });
            }
        }

        public override void CancelOperation(string cancelMessage)
        {
            try
            {
                base.CancelOperation(cancelMessage);
                //delete all or just those in process?
                //processStuff.ForEach(p => File.Delete(p.Output));
                if (!string.IsNullOrEmpty(tempFile))
                    File.Delete(tempFile);

                OnDownloadFinished(new FinishedEventArgs { Cancelled = Cancelled, Message = cancelMessage });
            }
            catch (Exception ex)
            {
                Failed = true;
                OnDownloadError(new ProgressEventArgs { Error = ex.Message });
            }
        }
        
        //private void CombineProcessOutputHandler(object sender, DataReceivedEventArgs outLine)
        //{
        //    if (Cancelled)
        //        return;

        //    var index = ProcessStuff.FindIndex(p => p.Process.Id == (sender as Process).Id);
        //    OnProgress(new ProgressEventArgs { Percentage = ProcessStuff[index].Percentage, Data = outLine.Data });
        //    if (string.IsNullOrEmpty(outLine.Data) || ProcessStuff[index].Finished || IsFinished(outLine.Data))
        //        return;

        //    LastData = outLine.Data;
        //    if (outLine.Data.Contains("ERROR") || outLine.Data.Contains("Invalid"))
        //    {
        //        Failed = true;
        //        OnDownloadError(new ProgressEventArgs { Error = outLine.Data });
        //        return;
        //    }

        //    if (!IsProcessing(outLine.Data))
        //        return;

        //    if (IsProcessing(outLine.Data))
        //    {
        //        var strSub = outLine.Data.Split(new[] { "time=" }, StringSplitOptions.RemoveEmptyEntries)[1].Substring(0, 11);
        //        ProcessStuff[index].CurrentTime = TimeSpan.Parse(strSub);
        //    }

        //    OnProgress(new ProgressEventArgs { Percentage = ProcessStuff[index].Percentage, Data = outLine.Data });

        //    // is it finished?
        //    if (ProcessStuff[index].Percentage < 100 && !IsFinished(outLine.Data))
        //        return;

        //    if (ProcessStuff[index].Percentage >= 100 && !ProcessStuff[index].Finished)
        //        OnProgress(new ProgressEventArgs { Percentage = ProcessStuff[index].Percentage, Data = outLine.Data });
        //}
        
        //private void CombineProcess_Exited(object sender, EventArgs e)
        //{
        //    var processClass = ProcessStuff.First(p => p.Process.Id == (sender as Process).Id);
        //    var index = ProcessStuff.FindIndex(p => p.Process.Id == processClass.Process.Id);
        //    if (Failed || Cancelled)
        //        return;

        //    CurrentProcess.Remove(processClass);
        //    ProcessStuff[index].Output = string.Empty; //so that we don't delete the merged output in CleanUp
        //    if (processClass.Process.ExitCode != 0 && !Cancelled)
        //    {
        //        OnDownloadError(new ProgressEventArgs { Error = LastData });
        //        return;
        //    }
        //    OnProgress(new ProgressEventArgs { Percentage = 100 });
        //    ProcessStuff[index].Finished = true;
        //    OnDownloadFinished(new FinishedEventArgs { Cancelled = Cancelled });
            
        //}
        private void OnSplitFinished(EventArgs e)
        {
            if (combineVideo)
                SplitFinished?.Invoke(this, e);
            else
                OnDownloadFinished(new FinishedEventArgs { Cancelled = Cancelled });
        }

        protected override void CleanUp()
        {
            foreach (var stuff in CurrentProcess)
            {
                if (!stuff.Process.HasExited)
                {
                    stuff.Process.Close();
                    Thread.Sleep(1000);
                }

                if (!combineVideo && !Cancelled)
                    continue;

                if (!string.IsNullOrEmpty(stuff.Output))
                    File.Delete(stuff.Output);
            }
            Thread.Sleep(1000);
            if (!string.IsNullOrEmpty(tempFile))
                File.Delete(tempFile);
        }

        protected override void OnProgress(ProgressEventArgs e) => ProgressDownload?.Invoke(this, e);

        protected override void OnDownloadFinished(FinishedEventArgs e)
        {
            FinishedDownload?.Invoke(this, e);
            CleanUp();
        }

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

            Failed = true;
            OnDownloadError(new ProgressEventArgs { Error = error.Data });
        }
    }
}
