using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using MVVMFramework;

namespace VideoUtilities
{
    public class VideoReverser : BaseClass<FileInfo>
    {
        public event ProgressEventHandler ProgressDownload;
        public event FinishedDownloadEventHandler FinishedDownload;
        public event StartedDownloadEventHandler StartedDownload;
        public event ErrorEventHandler ErrorDownload;
        public event MessageEventHandler MessageHandler;
        public event EventHandler<int> TrimFinished;
        public event EventHandler ReverseFinished;
        
        private readonly string sourceFolder;
        private readonly string filenameWithoutExtension;
        private readonly string fileExtension;
        private string tempFile;
        private readonly string fullInputPath;

        public VideoReverser(string fullPath)
        {
            fullInputPath = fullPath;
            sourceFolder = Path.GetDirectoryName(fullPath);
            filenameWithoutExtension = Path.GetFileNameWithoutExtension(fullPath);
            fileExtension = Path.GetExtension(fullPath);
            Failed = false;
            Cancelled = false;
        }

        public void TrimSections()
        {
            try
            {
                var output = $"{sourceFolder}\\{filenameWithoutExtension}_temp%03d{fileExtension}";
                OnDownloadStarted(new DownloadStartedEventArgs { Label = Translatables.TrimmingSectionsLabel });
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
                        Arguments = $"-y -i \"{fullInputPath}\" -map 0 -segment_time 7 -reset_timestamps 1 -f segment \"{output}\""
                    }
                };
                process.Exited += Process_Exited;
                process.ErrorDataReceived += OutputHandler;
                var stuff = new ProcessClass(false, process, output, TimeSpan.Zero, null);
                ProcessStuff.Add(stuff);
                CurrentProcess.Add(stuff);
                DoAfterExit = () =>
                {
                    ProcessStuff.Clear();
                    CurrentProcess.Clear();
                    NumberFinished = 0;
                    NumberInProcess = 0;
                    var regex = new Regex("_temp[0-9]{3}$");
                    var hdDirectoryInWhichToSearch = new DirectoryInfo(sourceFolder);
                    var filesInDir = hdDirectoryInWhichToSearch.GetFiles().Where(file => regex.IsMatch(Path.GetFileNameWithoutExtension(file.Name))).ToList();
                    SetList(filesInDir);
                    DoSetup(OnReverseFinished);
                    OnTrimFinished(filesInDir.Count);
                };
                process.Start();
                process.BeginErrorReadLine();
                while (!stuff.Finished)
                {
                    Thread.Sleep(200);
                }
            }
            catch (Exception ex)
            {
                Failed = true;
                OnDownloadError(new ProgressEventArgs { Error = ex.Message });
            }
        }

        private void OnTrimFinished(int count) => TrimFinished?.Invoke(this, count);
        private void OnReverseFinished() => ReverseFinished?.Invoke(this, EventArgs.Empty);

        protected override string CreateArguments(FileInfo obj, int index, string output)
            => $"-y -i \"{obj.FullName}\" -vf reverse -af areverse -map 0 \"{output}\"";

        protected override string CreateOutput(FileInfo obj, int index)
            => $"{sourceFolder}\\reversed_temp{index:000}{fileExtension}";

        protected override TimeSpan? GetDuration(FileInfo obj) => null;

        public void ConcatSections()
        {
            try
            {
                OnDownloadStarted(new DownloadStartedEventArgs { Label = Translatables.CombiningSectionsLabel });
                tempFile = Path.Combine(sourceFolder, $"temp_section_filenames{Guid.NewGuid()}.txt");
                using (var writeText = new StreamWriter(tempFile))
                {
                    var regex = new Regex("reversed_temp[0-9]{3}$");
                    var directory = new DirectoryInfo(sourceFolder);
                    var filesInDir = directory.GetFiles()
                        .Where(file => regex.IsMatch(Path.GetFileNameWithoutExtension(file.Name))).ToList();
                    for (var i = filesInDir.Count - 1; i >= 0; i--)
                        writeText.WriteLine($"file '{filesInDir[i].FullName}'");
                }

                var output = $"{sourceFolder}\\{filenameWithoutExtension}_reversed{fileExtension}";
                var duration = ProcessStuff.Aggregate(TimeSpan.Zero,
                    (current, processClass) => current + processClass.Duration.Value);
                var args = $"-y -safe 0 -f concat -i \"{tempFile}\" -c copy \"{output}\"";
                AddProcess(args, output, duration, true);
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
            if (!string.IsNullOrEmpty(tempFile))
                File.Delete(tempFile);
            OnDownloadFinished(new FinishedEventArgs { Cancelled = Cancelled, Message = cancelMessage });
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

                if (!Cancelled)
                    continue;

                if (!string.IsNullOrEmpty(stuff.Output))
                    File.Delete(stuff.Output);
            }
            Thread.Sleep(1000);
            var regex = new Regex("_temp[0-9]{3}$");
            var hdDirectoryInWhichToSearch = new DirectoryInfo(sourceFolder);
            var filesInDir = hdDirectoryInWhichToSearch.GetFiles().Where(file => regex.IsMatch(Path.GetFileNameWithoutExtension(file.Name)) || file.Name.Contains("temp_section_filenames")).ToList();
            filesInDir.Select(f => f.FullName).ToList().ForEach(File.Delete);
        }

        protected override void OnProgress(ProgressEventArgs e) => ProgressDownload?.Invoke(this, e);

        protected override void OnDownloadFinished(FinishedEventArgs e) => FinishedDownload?.Invoke(this, e);

        protected override void OnDownloadStarted(DownloadStartedEventArgs e) => StartedDownload?.Invoke(this, e);

        protected override void OnDownloadError(ProgressEventArgs e) => ErrorDownload?.Invoke(this, e);
    }
}