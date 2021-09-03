using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using MVVMFramework;
using MVVMFramework.ViewModels;

namespace VideoUtilities
{
    public class VideoSplitter : BaseClass<(TimeSpan StartTime, TimeSpan EndTime, string Title)>
    {
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
        private object _lock = new object();

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

        public override void Setup() => DoSetup(() => OnSplitFinished(EventArgs.Empty));

        protected override string CreateOutput((TimeSpan, TimeSpan, string) obj, int index) => $"{sourceFolder}\\{sourceFileWithoutExtension}_trimmed{index + 1}{(outputDifferentFormat ? outputFormat : extension)}";

        protected override string CreateArguments((TimeSpan StartTime, TimeSpan EndTime, string Title) obj, int index, ref string output)
        {
            var overwrite = false;
            if (File.Exists(output))
            {
                var args = new MessageEventArgs
                {
                    Message = $"The file {Path.GetFileName(output)} already exists. Overwrite? (Select \"No\" to output to a different file name.)"
                };
                ShowMessage(args);
                overwrite = args.Result;
                if (!overwrite)
                {
                    var filename = Path.GetFileNameWithoutExtension(output);
                    output = $"{Path.GetDirectoryName(output)}\\{filename}[0]{Path.GetExtension(output)}";
                }
            }
            return $"{(overwrite ? "-y" : string.Empty)} -i \"{fullInputPath}\" {(doReEncode ? string.Empty : "-codec copy")} -ss {obj.StartTime.TotalSeconds} -to {obj.EndTime.TotalSeconds} \"{output}\"";
        }

        protected override TimeSpan? GetDuration((TimeSpan StartTime, TimeSpan EndTime, string Title) obj) => obj.EndTime - obj.StartTime;

        public void CombineSections()
        {
            try
            {
                OnDownloadStarted(new DownloadStartedEventArgs { Label = Translatables.CombiningSectionsLabel });
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
