using System;
using System.Collections.Generic;
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
        
        private void OnSplitFinished(EventArgs e)
        {
            if (combineVideo)
                SplitFinished?.Invoke(this, e);
            else
                OnDownloadFinished(new FinishedEventArgs { Cancelled = Cancelled });
        }

        protected override void CleanUp()
        {
            foreach (var stuff in ProcessStuff)
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
    }
}
