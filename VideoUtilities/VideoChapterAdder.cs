using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using MVVMFramework;

namespace VideoUtilities
{
    public class VideoChapterAdder : BaseClass
    {
        public event ProgressEventHandler ProgressDownload;
        public event FinishedDownloadEventHandler FinishedDownload;
        public event StartedDownloadEventHandler StartedDownload;
        public event ErrorEventHandler ErrorDownload;

        private bool getFinished;
        private readonly ProcessStartInfo getMetadataStartInfo;
        private ProcessStartInfo setMetadataStartInfo;
        private readonly List<string> filenames = new List<string>();
        private decimal percentage;
        private bool cancelled;
        private TimeSpan duration;
        private Process getMetadataProcess;
        private Process setMetadataProcess;
        private string lastData;
        private bool failed;
        private readonly string metadataFile;
        private string chapterFile;
        private readonly string inputPath;
        private readonly string outputPath;
        private bool setFinished;

        public VideoChapterAdder(string fullInputPath, List<Tuple<TimeSpan, TimeSpan, string>> times = null, string importChapterFile = null)
        {
            cancelled = false;
            getFinished = false;
            var sourceFolder = Path.GetDirectoryName(fullInputPath);
            var sourceFileWithoutExtension = Path.GetFileNameWithoutExtension(fullInputPath);
            var extension = Path.GetExtension(fullInputPath);

            metadataFile = Path.Combine(sourceFolder, $"{sourceFileWithoutExtension}_metadataFile.txt");
            filenames.Add(metadataFile);
            if (string.IsNullOrEmpty(importChapterFile))
                CreateChapterFile(times, sourceFolder, sourceFileWithoutExtension);
            else
                chapterFile = importChapterFile;

            inputPath = Path.Combine(sourceFolder, $"{sourceFileWithoutExtension}{extension}");
            outputPath = Path.Combine(sourceFolder, $"{sourceFileWithoutExtension}_withchapters{extension}");
            getMetadataStartInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = Path.Combine(GetBinaryPath(), "ffmpeg.exe"),
                CreateNoWindow = true,
                Arguments = $"-y -i \"{inputPath}\" -f ffmetadata \"{metadataFile}\""
            };
        }

        public void AddChapters()
        {
            try
            {
                OnDownloadStarted(new DownloadStartedEventArgs { Label = Translatables.GettingMetadataMessage });
                getMetadataProcess = new Process
                {
                    EnableRaisingEvents = true,
                    StartInfo = getMetadataStartInfo
                };
                getMetadataProcess.Exited += Process_Exited;
                getMetadataProcess.ErrorDataReceived += GetMetadataOutputHandler;
                getMetadataProcess.Start();
                getMetadataProcess.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                failed = true;
                OnDownloadError(new ProgressEventArgs { Error = $"{ex.Message}\n{string.Format(Translatables.ChapterAdderTryAgain, chapterFile)}" });
                //if error occurs keep chapter file so it doesn't have to be redone
                filenames.Remove(chapterFile);
            }

            while (getFinished == false || setFinished == false)
            {
                Thread.Sleep(100); // wait while process exits;
            }
        }

        private void CreateChapterFile(List<Tuple<TimeSpan, TimeSpan, string>> times, string sourceFolder, string sourceFileWithoutExtension)
        {
            chapterFile = Path.Combine(sourceFolder, $"{sourceFileWithoutExtension}_chapters.txt");
            filenames.Add(chapterFile);
            using (var sw = new StreamWriter(chapterFile))
                foreach (var (startTime, _, title) in times)
                    sw.WriteLine($"{startTime},{title}");
        }

        private void WriteToMetadata()
        {
            using (var sw = new StreamWriter(metadataFile, true))
            {
                using (var sr = new StreamReader(chapterFile))
                {
                    string line;
                    var separators = new [] { ':', ',' };
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

        private void SetMetadata()
        {
            OnDownloadStarted(new DownloadStartedEventArgs { Label = Translatables.SettingMetadataMessage });

            setMetadataStartInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = Path.Combine(GetBinaryPath(), "ffmpeg.exe"),
                CreateNoWindow = true,
                Arguments = $"-y -i \"{inputPath}\" -i \"{metadataFile}\" -map_metadata 1 -codec copy \"{outputPath}\""
            };
            
            setMetadataProcess = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = setMetadataStartInfo
            };
            setMetadataProcess.Exited += SetMetadataProcess_Exited;
            setMetadataProcess.ErrorDataReceived += SetMetadataProcess_ErrorDataReceived;

            setMetadataProcess.Start();
            setMetadataProcess.BeginErrorReadLine();
        }

        public override void CancelOperation(string cancelMessage)
        {
            cancelled = true;
            if (!getMetadataProcess.HasExited)
                getMetadataProcess.Kill();
            if (!setMetadataProcess.HasExited)
                setMetadataProcess.Kill();

            Thread.Sleep(1000);
            filenames.ForEach(File.Delete);
            OnDownloadFinished(new FinishedEventArgs { Cancelled = cancelled, Message = cancelMessage });
        }

        private void GetMetadataOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (cancelled)
                return;

            //OnProgress(new ProgressEventArgs { Percentage = getFinished ? 0 : 100, Data = getFinished ? string.Empty : outLine.Data });
            if (string.IsNullOrEmpty(outLine.Data) || getFinished || IsFinished(outLine.Data))
                return;

            lastData = outLine.Data;
            if (outLine.Data.Contains("ERROR") || outLine.Data.Contains("Invalid"))
            {
                failed = true;
                OnDownloadError(new ProgressEventArgs { Error = outLine.Data });
            }
        }

        private void SetMetadataProcess_ErrorDataReceived(object sender, DataReceivedEventArgs outLine)
        {
            if (cancelled)
                return;

            OnProgress(new ProgressEventArgs { Percentage = setFinished ? 0 : percentage, Data = setFinished ? string.Empty : outLine.Data });
            if (string.IsNullOrEmpty(outLine.Data) || setFinished || IsFinished(outLine.Data))
                return;

            lastData = outLine.Data;
            if (outLine.Data.Contains("ERROR") || outLine.Data.Contains("Invalid"))
            {
                failed = true;
                OnDownloadError(new ProgressEventArgs { Error = $"{outLine.Data}\n{string.Format(Translatables.ChapterAdderTryAgain, chapterFile)}" });
                //if error occurs keep chapter file so it doesn't have to be redone
                filenames.Remove(chapterFile);
                return;
            }

            if (!outLine.Data.Contains("Duration: ") && !IsProcessing(outLine.Data))
                return;

            if (outLine.Data.Contains("Duration: "))
            {
                if (duration == TimeSpan.Zero)
                    duration = TimeSpan.Parse(outLine.Data.Split(new[] { "Duration: " }, StringSplitOptions.None)[1].Substring(0, 11));
                return;
            }

            var currentTime = TimeSpan.Zero;
            if (IsProcessing(outLine.Data))
            {
                var strSub = outLine.Data.Split(new[] { "time=" }, StringSplitOptions.RemoveEmptyEntries)[1].Substring(0, 11);
                currentTime = TimeSpan.Parse(strSub);
            }

            // fire the process event
            var perc = Convert.ToDecimal((float)currentTime.TotalSeconds / (float)duration.TotalSeconds) * 100;
            if (perc < 0)
            {
                Console.WriteLine($"weird percentage {perc}");
                return;
            }
            percentage = perc;
            OnProgress(new ProgressEventArgs { Percentage = perc, Data = outLine.Data });

            // is it finished?
            if (perc < 100 && !IsFinished(outLine.Data))
                return;

            if (perc >= 100 && !setFinished)
                OnProgress(new ProgressEventArgs { Percentage = percentage, Data = outLine.Data });
        }

        private static bool IsProcessing(string data) => data.Contains("frame=") && data.Contains("fps=") && data.Contains("time=");
        private static bool IsFinished(string data) => data.Contains("global headers:") && data.Contains("muxing overhead:");

        protected override void Process_Exited(object sender, EventArgs e)
        {
            if (getMetadataProcess.ExitCode != 0 || failed || cancelled)
            {
                if (getMetadataProcess.ExitCode != 0 && !cancelled)
                    OnDownloadError(new ProgressEventArgs { Error = lastData });
                CleanUp();
                return;
            }

            getFinished = true;
            WriteToMetadata();
            SetMetadata();
        }

        private void SetMetadataProcess_Exited(object sender, EventArgs e)
        {
            if (setMetadataProcess.ExitCode != 0 || failed || cancelled)
            {
                if (setMetadataProcess.ExitCode != 0 && !cancelled)
                {
                    OnDownloadError(new ProgressEventArgs { Error = $"{lastData}\n{string.Format(Translatables.ChapterAdderTryAgain, chapterFile)}" });
                    //if error occurs keep chapter file so it doesn't have to be redone
                    filenames.Remove(chapterFile);
                }
                CleanUp();
                return;
            }

            setFinished = true;
            FinishedDownload?.Invoke(this, new FinishedEventArgs { Cancelled = cancelled });
            CleanUp();
        }

        private void CleanUp()
        {
            if (!getMetadataProcess.HasExited)
                getMetadataProcess.Close();
            if (!setMetadataProcess.HasExited)
                setMetadataProcess.Close();

            Thread.Sleep(1000);
            filenames.ForEach(File.Delete);
        }

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