using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using MVVMFrameworkNet472.Localization;

namespace VideoUtilities
{
    public class VideoChapterAdder : BaseClass
    {
        private readonly string metadataFile;
        private string chapterFile;
        private readonly string fullInputPath;
        private readonly string sourceFolder;
        private readonly string sourceFileWithoutExtension;
        private readonly string extension;
        private readonly List<SectionViewModel> timeList;
        private bool deleteChapterFile;

        public VideoChapterAdder(ChapterAdderArgs args) : base(args.InputPaths)
        {
            OutputPath = args.InputPaths[0];
            fullInputPath = args.InputPaths[0];
            chapterFile = args.ImportChapterFile;
            timeList = args.Sections;
            deleteChapterFile = args.DeleteChapterFile;
            sourceFolder = Path.GetDirectoryName(fullInputPath);
            sourceFileWithoutExtension = Path.GetFileNameWithoutExtension(fullInputPath);
            extension = Path.GetExtension(fullInputPath);
            metadataFile = Path.Combine(sourceFolder, $"{sourceFileWithoutExtension}_metadataFile.txt");
        }

        public override void Setup()
        {
            DoSetup(() =>
            {
                WriteToMetadata();
                OnFirstWorkFinished(EventArgs.Empty);
            });
        }

        public override void PreWork()
        {
            if (string.IsNullOrEmpty(chapterFile))
                CreateChapterFile(timeList, sourceFolder, sourceFileWithoutExtension);
        }

        protected override string CreateOutput(int index, object obj) => metadataFile;

        protected override string CreateArguments(int index, ref string output, object obj)
            => $"-y -i \"{obj}\" -f ffmetadata \"{metadataFile}\"";

        protected override TimeSpan? GetDuration(object obj) => null;

        private void CreateChapterFile(List<SectionViewModel> times, string sourceFolder, string sourceFileWithoutExtension)
        {
            chapterFile = Path.Combine(sourceFolder, $"{sourceFileWithoutExtension}_chapters.txt");
            using (var sw = new StreamWriter(chapterFile))
                foreach (var time in times)
                    sw.WriteLine($"{time.StartTime},{time.Title}");
        }

        public override void SecondaryWork()
        {
            try
            {
                OnDownloadStarted(new DownloadStartedEventArgs { Label = new SettingMetadataMessageTranslatable() });
                var outputPath = Path.Combine(sourceFolder, $"{sourceFileWithoutExtension}_withchapters{extension}");
                var args = $"{(CheckOverwrite(ref outputPath) ? "-y" : string.Empty)} -i \"{fullInputPath}\" -i \"{metadataFile}\" -map_metadata 1 -codec copy \"{outputPath}\"";
                AddProcess(args, outputPath, null, true);
            }
            catch (Exception ex)
            {
                Failed = true;
                OnDownloadError(new ProgressEventArgs { Error = ex.Message });
            }
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
            if (deleteChapterFile)
                File.Delete(chapterFile);
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