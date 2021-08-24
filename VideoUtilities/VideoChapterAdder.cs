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
        private readonly string sourceFolder;
        private readonly string sourceFileWithoutExtension;
        private readonly string extension;
        private readonly List<Tuple<TimeSpan, TimeSpan, string>> times;
        private readonly ProcessStartInfo getMetadataStartInfo;
        private ProcessStartInfo setMetadataStartInfo;
        private readonly List<string> filenames = new List<string>();
        private decimal percentage;
        private bool cancelled;
        private TimeSpan newDur = TimeSpan.Zero;
        private Process getMetadataProcess;
        private Process setMetadataProcess;
        private string lastData;
        private bool failed;
        private bool combineFinished;
        private readonly string metadataFile;
        private readonly string chapterFile;
        private readonly string inputPath;
        private readonly string outputPath;
        private readonly string libsPath;

        public VideoChapterAdder(string sFolder, string sFileWithoutExtension, string ext, List<Tuple<TimeSpan, TimeSpan, string>> t)
        {
            cancelled = false;
            getFinished = false;
            sourceFolder = sFolder;
            sourceFileWithoutExtension = sFileWithoutExtension;
            extension = ext;
            times = t;

            var directoryName = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            if (directoryName != null)
                libsPath = Path.Combine(directoryName, "Binaries", "CSPlugins", "FFmpeg", IntPtr.Size == 8 ? "x64" : "x86");
            if (string.IsNullOrEmpty(libsPath))
                throw new Exception("Cannot read 'binaryFolder' variable from app.config / web.config.");

            metadataFile = Path.Combine(sourceFolder, $"{sourceFileWithoutExtension}_metadataFile.txt");
            chapterFile = Path.Combine(sourceFolder, $"{sourceFileWithoutExtension}_chapters.txt");
            filenames.AddRange(new[] { metadataFile, chapterFile });
            //create chapter file
            using (var sw = new StreamWriter(chapterFile))
                foreach (var (startTime, _, title) in times)
                    sw.WriteLine($"{startTime},{title}");

            inputPath = Path.Combine(sourceFolder, $"{sourceFileWithoutExtension}{extension}");
            outputPath = Path.Combine(sourceFolder, $"{sourceFileWithoutExtension}_withchapters{extension}");
            getMetadataStartInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = Path.Combine(libsPath, "ffmpeg.exe"),
                CreateNoWindow = true,
                Arguments = $"-i \"{inputPath}\" -f ffmetadata \"{metadataFile}\""
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
                OnDownloadError(new ProgressEventArgs { Error = ex.Message });
            }

            while (getFinished == false || combineFinished == false)
            {
                Thread.Sleep(100); // wait while process exits;
            }
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
                    for (int i = 0; i < chapters.Count - 1; i++)
                    {
                        sw.WriteLine("[CHAPTER]");
                        sw.WriteLine("TIMEBASE=1/1000");
                        sw.WriteLine($"START={chapters[i].Timestamp}");
                        sw.WriteLine($"END={chapters[i + 1].Timestamp - 1}");
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
                FileName = Path.Combine(libsPath, "ffmpeg.exe"),
                CreateNoWindow = true,
                Arguments = $"-i \"{inputPath}\" -i \"{metadataFile}\" -map_metadata 1 -codec copy \"{outputPath}\""
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

            OnProgress(new ProgressEventArgs { Percentage = getFinished ? 0 : percentage, Data = getFinished ? string.Empty : outLine.Data });
            if (string.IsNullOrEmpty(outLine.Data) || getFinished || IsFinished(outLine.Data))
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

            var currentTime = TimeSpan.Zero;
            if (IsProcessing(outLine.Data))
            {
                var strSub = outLine.Data.Split(new[] { "time=" }, StringSplitOptions.RemoveEmptyEntries)[1].Substring(0, 11);
                currentTime = TimeSpan.Parse(strSub);
            }

            // fire the process event
            var perc = Convert.ToDecimal((float)currentTime.TotalSeconds / (float)newDur.TotalSeconds) * 100;
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

            if (perc >= 100 && !getFinished)
                OnProgress(new ProgressEventArgs { Percentage = percentage, Data = outLine.Data });
        }

        private void SetMetadataProcess_ErrorDataReceived(object sender, DataReceivedEventArgs outLine)
        {
            if (cancelled)
                return;

            if (string.IsNullOrEmpty(outLine.Data) || combineFinished || IsFinished(outLine.Data))
            {
                OnProgress(new ProgressEventArgs { Percentage = 100, Data = outLine.Data });
                return;
            }

            lastData = outLine.Data;
            if (!outLine.Data.Contains("ERROR") && !outLine.Data.Contains("Invalid"))
                return;

            failed = true;
            OnDownloadError(new ProgressEventArgs { Error = outLine.Data });
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
            if (getMetadataProcess.ExitCode != 0 || failed || cancelled)
            {
                if (getMetadataProcess.ExitCode != 0 && !cancelled)
                    OnDownloadError(new ProgressEventArgs { Error = lastData });
                CleanUp();
                return;
            }

            combineFinished = true;
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
            var secs = int.Parse(split[2]);
            Title = split[3];

            Minutes = (hrs * 60) + mins;
            Seconds = secs + (Minutes * 60);
            Timestamp = Seconds * 1000;
        }
    }
}