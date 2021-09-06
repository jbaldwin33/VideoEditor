using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using MVVMFramework.Localization;

namespace VideoUtilities
{
    public class VideoSplitter : BaseClass
    {
        public event EventHandler SplitFinished;

        private readonly string sourceFolder;
        private readonly string sourceFileWithoutExtension;
        private readonly string extension;
        private readonly bool outputDifferentFormat;
        private readonly string outputFormat;
        private readonly bool combineVideo;
        private readonly string fullInputPath;
        private readonly bool doReEncode;
        private string tempFile;
        private readonly object _lock = new object();

        public VideoSplitter(List<(TimeSpan, TimeSpan, string)> times, string fullPath, bool combine, bool outputDiffFormat, string outFormat, bool reEncodeVideo)
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
            SetList(times);
        }

        public override void Setup() => DoSetup(() => OnFirstWorkFinished(EventArgs.Empty));
        protected override string CreateArguments(int index, ref string output, object obj)
        {
            var (startTime, endTime, _) = (ValueTuple<TimeSpan, TimeSpan, string>)obj;
            return $"{(CheckOverwrite(ref output) ? "-y" : string.Empty)} -i \"{fullInputPath}\" {(doReEncode ? string.Empty : "-codec copy")} -ss {startTime.TotalSeconds} -to {endTime.TotalSeconds} \"{output}\"";
        }

        protected override string CreateOutput(int index, object obj) => $"{sourceFolder}\\{sourceFileWithoutExtension}_trimmed{index + 1}{(outputDifferentFormat ? outputFormat : extension)}";

        protected override TimeSpan? GetDuration(object obj)
        {
            var (startTime, endTime, _) = (ValueTuple<TimeSpan, TimeSpan, string>)obj;
            return endTime - startTime;
        }

        public override void SecondaryWork()
        {
            try
            {
                OnDownloadStarted(new DownloadStartedEventArgs { Label = new CombiningSectionsLabelTranslatable() });
                tempFile = Path.Combine(sourceFolder, $"temp_section_filenames{Guid.NewGuid()}.txt");
                File.WriteAllLines(tempFile, ProcessStuff.Select(x => $"file '{x.Output}'"));
                var combinedFile = $"{sourceFolder}\\{sourceFileWithoutExtension}_combined{(outputDifferentFormat ? outputFormat : extension)}";
                var args = $"-safe 0 -f concat -i \"{tempFile}\" -c copy \"{combinedFile}\"";
                var duration = ProcessStuff.Aggregate(TimeSpan.Zero, (current, processClass) => current + processClass.Duration.Value);
                AddProcess(args, combinedFile, duration, true);
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
            }
            catch (Exception ex)
            {
                Failed = true;
                OnDownloadError(new ProgressEventArgs { Error = ex.Message });
            }
        }

        protected override void OnFirstWorkFinished(EventArgs e)
        {
            if (combineVideo)
                SplitFinished?.Invoke(this, e);
            else
                OnDownloadFinished(new FinishedEventArgs { Cancelled = Cancelled });
        }

        protected override void CleanUp()
        {
            lock (_lock)
            {
                foreach (var stuff in ProcessStuff)
                {
                    CloseProcess(stuff.Process, false);

                    if (!combineVideo && !Cancelled)
                        continue;

                    if (!string.IsNullOrEmpty(stuff.Output))
                        File.Delete(stuff.Output);
                }
            }

            Thread.Sleep(1000);
            if (!string.IsNullOrEmpty(tempFile))
                File.Delete(tempFile);
        }
    }
}
