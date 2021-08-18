using Microsoft.VisualBasic.Devices;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace VideoUtilities
{
    public class VideoReverser : BaseClass
    {
        private string output;
        public event ProgressEventHandler ProgressDownload;
        public event FinishedDownloadEventHandler FinishedDownload;
        public event StartedDownloadEventHandler StartedDownload;
        public event ErrorEventHandler ErrorDownload;
        public event MessageEventHandler MessageHandler; 
        private Process trimProcess;
        private ProcessStartInfo trimStartInfo;
        private Process reverseProcess;
        private ProcessStartInfo reverseStartInfo;
        private Process concatProcess;
        private ProcessStartInfo concatStartInfo;
        private bool finished;
        private decimal percentage;
        private TimeSpan duration;
        private bool cancelled;
        private bool failed;
        private string lastData;
        private readonly string sourceFolder;
        private readonly string filenameWithoutExtension;
        private readonly string fileExtension;
        private readonly string binaryPath;

        public VideoReverser(string folder, string fileWithoutExtension, string extension)
        {
            sourceFolder = folder;
            filenameWithoutExtension = fileWithoutExtension;
            fileExtension = extension;
            failed = false;
            cancelled = false;
            binaryPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Binaries");
            if (string.IsNullOrEmpty(binaryPath))
                throw new Exception("Cannot read 'binaryFolder' variable from app.config / web.config.");

            //for trimming
            output = $"{folder}\\{fileWithoutExtension}_temp%03d{extension}";
            trimStartInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = Path.Combine(binaryPath, "ffmpeg.exe"),
                CreateNoWindow = true,
                Arguments = $"-i \"{folder}\\{fileWithoutExtension}{extension}\" -map 0 -segment_time 7 -reset_timestamps 1 -f segment \"{output}\""
            };

        }

        public void ReverseVideo() => TrimSections();

        public void TrimSections()
        {
            try
            {
                OnDownloadStarted(new DownloadStartedEventArgs { Label = "Trimming video into sections..." });
                trimProcess = new Process
                {
                    EnableRaisingEvents = true,
                    StartInfo = trimStartInfo
                };
                trimProcess.Exited += Process_Exited;
                trimProcess.ErrorDataReceived += OutputHandler;
                trimProcess.Start();
                trimProcess.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                failed = true;
                OnDownloadError(new ProgressEventArgs { Error = ex.Message });
            }
        }

        private void ReverseSections()
        {
            reverseStartInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = Path.Combine(binaryPath, "ffmpeg.exe"),
                CreateNoWindow = true
            };
            //get files
            var regex = new Regex("_temp[0-9]{3}$");
            var hdDirectoryInWhichToSearch = new DirectoryInfo(sourceFolder);
            var filesInDir = hdDirectoryInWhichToSearch.GetFiles().Where(file => regex.IsMatch(Path.GetFileNameWithoutExtension(file.Name))).ToList();
            var sb = new StringBuilder();
            for (var i = 0; i < filesInDir.Count; i++)
            {
                output = $"{sourceFolder}\\reversed_temp{i:000}{fileExtension}";
                sb.Append($"-i \"{filesInDir[i].FullName}\" -vf reverse -af areverse -map {i} \"{output}\" ");
            }

            reverseStartInfo.Arguments = sb.ToString();

            OnDownloadStarted(new DownloadStartedEventArgs { Label = "Reversing sections..." });
            reverseProcess = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = reverseStartInfo
            };
            reverseProcess.Exited += ReverseProcess_Exited;
            reverseProcess.ErrorDataReceived += OutputHandler;
            reverseProcess.Start();
            reverseProcess.BeginErrorReadLine();
        }

        private void ConcatSections()
        {
            concatStartInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = Path.Combine(binaryPath, "ffmpeg.exe"),
                CreateNoWindow = true
            };

            OnDownloadStarted(new DownloadStartedEventArgs { Label = "Combining sections..." });
            var tempFile = Path.Combine(sourceFolder, $"temp_section_filenames{Guid.NewGuid()}.txt");
            using (var writeText = new StreamWriter(tempFile))
            {
                var regex = new Regex("reversed_temp[0-9]{3}$");
                var directory = new DirectoryInfo(sourceFolder);
                var filesInDir = directory.GetFiles().Where(file => regex.IsMatch(Path.GetFileNameWithoutExtension(file.Name))).ToList();
                for (var i = filesInDir.Count - 1; i >= 0; i--)
                    writeText.WriteLine($"file '{filesInDir[i].FullName}'");
            }

            var reversedFile = $"{sourceFolder}\\{filenameWithoutExtension}_reversed{fileExtension}";
            concatStartInfo.Arguments = $"-safe 0 -f concat -i \"{tempFile}\" -c copy \"{reversedFile}\"";
            concatProcess = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = concatStartInfo
            };
            concatProcess.Exited += ConcatProcess_Exited;
            concatProcess.ErrorDataReceived += ConcatProcess_OutputHandler;
            concatProcess.Start();
            concatProcess.BeginErrorReadLine();
        }

        private void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (cancelled)
                return;
            var info = new ComputerInfo();
            var availableMemory = info.AvailablePhysicalMemory / (1024f * 1024f * 1024f);
            var totalMemory = info.TotalPhysicalMemory / (1024f * 1024f * 1024f);
            var usedMemoryPercentage = (totalMemory - availableMemory) / totalMemory * 100;

            if (usedMemoryPercentage > 95)
                CancelOperation($"RAM usage exceeds {usedMemoryPercentage}%.");

            OnProgress(new ProgressEventArgs { Percentage = finished ? 0 : percentage, Data = finished ? string.Empty : outLine.Data });
            // extract the percentage from process output
            if (string.IsNullOrEmpty(outLine.Data) || finished || IsFinished(outLine.Data))
                return;

            lastData = outLine.Data;
            if (outLine.Data.Contains("ERROR"))
            {
                failed = true;
                OnDownloadError(new ProgressEventArgs { Error = outLine.Data });
                return;
            }

            if (!outLine.Data.Contains("Duration: ") && !isConverting())
                return;

            if (outLine.Data.Contains("Duration: "))
            {
                duration = TimeSpan.Parse(outLine.Data.Split(new[] { "Duration: " }, StringSplitOptions.None)[1].Substring(0, 11));
                if (duration > TimeSpan.FromMinutes(1))
                {
                    var args = new MessageEventArgs { Message = "Video file exceeds 1 minute. If you continue it is possible your computer will run out of memory during the process and freeze. Do you still want to continue?" };
                    OnShowMessage(args);
                    if (!args.Result)
                        CancelOperation("");
                }
                return;
            }

            var currentTime = TimeSpan.Zero;
            if (isConverting())
            {
                var strSub = outLine.Data.Split(new[] { "time=" }, StringSplitOptions.RemoveEmptyEntries)[1].Substring(0, 11);
                currentTime = TimeSpan.Parse(strSub);
            }

            // fire the process event
            var perc = Convert.ToDecimal((float)currentTime.TotalSeconds / (float)duration.TotalSeconds) * 100;
            if (reverseProcess != null)
            {

            }
            if (perc < 0)
            {
                Console.WriteLine("weird perc {0}", perc);
                return;
            }
            percentage = perc;
            OnProgress(new ProgressEventArgs { Percentage = perc, Data = outLine.Data });

            // is it finished?
            if (perc < 100 && !IsFinished(outLine.Data))
                return;

            if (perc >= 100 && !finished)
                OnProgress(new ProgressEventArgs { Percentage = percentage, Data = outLine.Data });

            bool isConverting() => outLine.Data.Contains("frame=") && outLine.Data.Contains("fps=") && outLine.Data.Contains("time=");
        }

        private void ConcatProcess_OutputHandler(object sender, DataReceivedEventArgs outLine)
        {
            if (cancelled)
                return;

            if (string.IsNullOrEmpty(outLine.Data) || finished || IsFinished(outLine.Data))
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

        protected override void Process_Exited(object sender, EventArgs e)
        {
            if (finished || failed || cancelled)
                return;

            if (trimProcess.ExitCode != 0 && !cancelled)
            {
                OnDownloadError(new ProgressEventArgs { Error = lastData });
                return;
            }

            ReverseSections();
        }

        protected void ReverseProcess_Exited(object sender, EventArgs e)
        {
            if (finished || failed || cancelled)
                return;

            if (reverseProcess.ExitCode != 0 && !cancelled)
            {
                OnDownloadError(new ProgressEventArgs { Error = lastData });
                return;
            }

            ConcatSections();
        }

        protected void ConcatProcess_Exited(object sender, EventArgs e)
        {
            if (finished || failed || cancelled)
                return;

            if (concatProcess.ExitCode != 0 && !cancelled)
            {
                OnDownloadError(new ProgressEventArgs { Error = lastData });
                return;
            }

            finished = true;
            OnDownloadFinished(new FinishedEventArgs { Cancelled = cancelled });
            CleanUp();
        }

        public override void CancelOperation(string cancelMessage)
        {
            cancelled = true;
            if (trimProcess != null && !trimProcess.HasExited)
            {
                trimProcess.Kill();
                Thread.Sleep(1000);
            }
            if (reverseProcess != null && !reverseProcess.HasExited)
            {
                reverseProcess.Kill();
                Thread.Sleep(1000);
            }
            if (concatProcess != null && !concatProcess.HasExited)
            {
                concatProcess.Kill();
                Thread.Sleep(1000);
            }

            OnDownloadFinished(new FinishedEventArgs { Cancelled = cancelled, Message = cancelMessage });
            CleanUp();
        }

        private void CleanUp()
        {
            Thread.Sleep(1000);
            var regex = new Regex("_temp[0-9]{3}$");
            var hdDirectoryInWhichToSearch = new DirectoryInfo(sourceFolder);
            var filesInDir = hdDirectoryInWhichToSearch.GetFiles().Where(file => regex.IsMatch(Path.GetFileNameWithoutExtension(file.Name)) || file.Name.Contains("temp_section_filenames")).ToList();
            filesInDir.Select(f => f.FullName).ToList().ForEach(File.Delete);
        }

        private bool IsFinished(string data) => data.Contains("global headers:") && data.Contains("muxing overhead:");


        protected override void OnProgress(ProgressEventArgs e) => ProgressDownload?.Invoke(this, e);

        protected override void OnDownloadFinished(FinishedEventArgs e) => FinishedDownload?.Invoke(this, e);

        protected override void OnDownloadStarted(DownloadStartedEventArgs e) => StartedDownload?.Invoke(this, e);

        protected override void OnDownloadError(ProgressEventArgs e) => ErrorDownload?.Invoke(this, e);

        protected override void ErrorDataReceived(object sendingProcess, DataReceivedEventArgs error)
        {
            if (string.IsNullOrEmpty(error.Data))
                return;

            failed = true;
            OnDownloadError(new ProgressEventArgs { Error = error.Data });
        }

        protected override void OnShowMessage(MessageEventArgs e) => MessageHandler?.Invoke(this, e);
    }
}