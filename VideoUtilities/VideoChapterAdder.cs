using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using MVVMFramework;
using MVVMFramework.Localization;

namespace VideoUtilities
{
    public class VideoChapterAdder : BaseClass<string>
    {
        public event EventHandler GetMetadataFinished;
        private readonly string metadataFile;
        private string chapterFile;
        private readonly string fullInputPath;
        private readonly string sourceFolder;
        private readonly string sourceFileWithoutExtension;
        private readonly string extension;

        public VideoChapterAdder(string fullPath, List<Tuple<TimeSpan, TimeSpan, string>> times = null, string importChapterFile = null)
        {
            Cancelled = false;
            fullInputPath = fullPath;
            sourceFolder = Path.GetDirectoryName(fullInputPath);
            sourceFileWithoutExtension = Path.GetFileNameWithoutExtension(fullInputPath);
            extension = Path.GetExtension(fullInputPath);
            metadataFile = Path.Combine(sourceFolder, $"{sourceFileWithoutExtension}_metadataFile.txt");
            if (string.IsNullOrEmpty(importChapterFile))
                CreateChapterFile(times, sourceFolder, sourceFileWithoutExtension);
            else
                chapterFile = importChapterFile;
            SetList(new[] { fullPath });
        }

        public override void Setup()
        {
            DoSetup(() =>
            {
                WriteToMetadata();
                OnGetMetadataFinished();
            });
        }

        protected override string CreateOutput(string obj, int index) => metadataFile;

        protected override string CreateArguments(string obj, int index, ref string output)
            => $"-y -i \"{obj}\" -f ffmetadata \"{metadataFile}\"";

        protected override TimeSpan? GetDuration(string obj) => null;

        private void CreateChapterFile(List<Tuple<TimeSpan, TimeSpan, string>> times, string sourceFolder, string sourceFileWithoutExtension)
        {
            chapterFile = Path.Combine(sourceFolder, $"{sourceFileWithoutExtension}_chapters.txt");
            using (var sw = new StreamWriter(chapterFile))
                foreach (var (startTime, _, title) in times)
                    sw.WriteLine($"{startTime},{title}");
        }

        public void SetMetadata()
        {
            try
            {
                OnDownloadStarted(new DownloadStartedEventArgs { Label = new SettingMetadataMessageTranslatable() });
                var outputPath = Path.Combine(sourceFolder, $"{sourceFileWithoutExtension}_withchapters{extension}");
                var overwrite = false;
                if (File.Exists(outputPath))
                {
                    var args2 = new MessageEventArgs
                    {
                        Message = $"The file {Path.GetFileName(outputPath)} already exists. Overwrite? (Select \"No\" to output to a different file name.)"
                    };
                    ShowMessage(args2);
                    overwrite = args2.Result;
                    if (!overwrite)
                    {
                        var filename = Path.GetFileNameWithoutExtension(outputPath);
                        outputPath = $"{Path.GetDirectoryName(outputPath)}\\{filename}[0]{Path.GetExtension(outputPath)}";
                    }
                }
                var args = $"{(overwrite ? "-y" : string.Empty)} -i \"{fullInputPath}\" -i \"{metadataFile}\" -map_metadata 1 -codec copy \"{outputPath}\"";
                AddProcess(args, outputPath, null, true);
            }
            catch (Exception ex)
            {
                Failed = true;
                OnDownloadError(new ProgressEventArgs { Error = ex.Message });
            }
        }

        public override void CancelOperation(string cancelMessage)
        {
            base.CancelOperation(cancelMessage);
            OnDownloadFinished(new FinishedEventArgs { Cancelled = Cancelled, Message = cancelMessage });
        }

        protected override void CleanUp()
        {
            foreach (var stuff in ProcessStuff)
            {
                CloseProcess(stuff.Process, false);
                if (!Cancelled)
                    continue;

                if (!string.IsNullOrEmpty(stuff.Output))
                    File.Delete(stuff.Output);
            }
            Thread.Sleep(1000);
            if (!string.IsNullOrEmpty(metadataFile))
                File.Delete(metadataFile);
        }

        private void WriteToMetadata()
        {
            using (var sw = new StreamWriter(metadataFile, true))
            {
                using (var sr = new StreamReader(chapterFile))
                {
                    string line;
                    var separators = new[] { ':', ',' };
                    var chapters = new List<Chapter>();
                    while ((line = sr.ReadLine()) != null)
                    {
                        var split = line.Split(separators);
                        chapters.Add(new Chapter(line, split));
                    }
                    for (int i = 0; i < chapters.Count; i++)
                    {
                        sw.WriteLine("[CHAPTER]");
                        sw.WriteLine("TIMEBASE=1/1000");
                        sw.WriteLine($"START={chapters[i].Timestamp}");
                        sw.WriteLine($"END={(i + 1 >= chapters.Count ? chapters[i].Timestamp : chapters[i + 1].Timestamp - 1)}");
                        sw.WriteLine($"title={chapters[i].Title}");
                    }
                }
            }
        }

        private void OnGetMetadataFinished() => GetMetadataFinished?.Invoke(this, EventArgs.Empty);
    }

    public class Chapter
    {
        public string Line;
        public int Minutes;
        public int Seconds;
        public int Timestamp;
        public string Title;

        public Chapter(string line, string[] split)
        {
            Line = line;
            SetProps(split);
        }

        public void SetProps(string[] split)
        {
            var hrs = int.Parse(split[0]);
            var mins = int.Parse(split[1]);
            var secs = (int)decimal.Parse(split[2]);
            Title = split[3];

            Minutes = (hrs * 60) + mins;
            Seconds = secs + (Minutes * 60);
            Timestamp = Seconds * 1000;
        }
    }
}