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
        private readonly bool outputDifferentFormat;
        private readonly string outputFormat;
        private readonly bool combineVideo;
        private readonly string fullInputPath;
        private readonly bool doReEncode;
        private string tempFile;
        private string sourceFolder => Path.GetDirectoryName(fullInputPath);
        private string sourceFileWithoutExtension => Path.GetFileNameWithoutExtension(fullInputPath);
        private string extension => Path.GetExtension(fullInputPath);
        private readonly object _lock = new object();

        public VideoSplitter(SplitterArgs args) : base(args.List)
        {
            fullInputPath = args.InputPaths[0];
            doReEncode = args.ReEncodeVideo;
            combineVideo = args.CombineVideo;
            outputDifferentFormat = args.OutputDifferentFormat;
            outputFormat = args.OutputFormat;
            OutputPath = $"{sourceFolder}\\{sourceFileWithoutExtension}_trimmed1{(outputDifferentFormat ? outputFormat : extension)}";
        }

        public override void Setup() => DoSetup(() => OnFirstWorkFinished(EventArgs.Empty));
        protected override string CreateArguments(int index, ref string output, object obj)
        {
            var args = (SectionViewModel)obj;
            return $"{(CheckOverwrite(ref output) ? "-y" : string.Empty)} -i \"{fullInputPath}\" {(doReEncode ? string.Empty : "-codec copy")} -ss {args.StartTime.TotalSeconds} -to {args.EndTime.TotalSeconds} \"{output}\"";
        }

        protected override string CreateOutput(int index, object obj) => $"{sourceFolder}\\{sourceFileWithoutExtension}_trimmed{index + 1}{(outputDifferentFormat ? outputFormat : extension)}";

        protected override TimeSpan? GetDuration(object obj)
        {
            var args = (SectionViewModel)obj;
            return args.EndTime - args.StartTime;
        }

        public override void SecondaryWork()
        {
            try
            {
                OnDownloadStarted(new DownloadStartedEventArgs { Label = new CombiningSectionsLabelTranslatable() });
                tempFile = Path.Combine(sourceFolder, $"temp_section_filenames{Guid.NewGuid()}.txt");
                File.WriteAllLines(tempFile, ProcessStuff.Select(x => $"file '{x.Output}'"));
                var combinedFile = $"{sourceFolder}\\{sourceFileWithoutExtension}_combined{(outputDifferentFormat ? outputFormat : extension)}";
                var args = $"{(CheckOverwrite(ref combinedFile) ? "-y" : string.Empty)} -safe 0 -f concat -i \"{tempFile}\" -c copy \"{combinedFile}\"";
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
                base.OnFirstWorkFinished(e);
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
