using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

namespace VideoUtilities
{
    public class VideoSplitter : BaseClass
    {
        public event ProgressEventHandler ProgressDownload;
        public event FinishedDownloadEventHandler FinishedDownload;
        public event StartedDownloadEventHandler StartedDownload;
        public event ErrorEventHandler ErrorDownload;

        private bool finished;
        private readonly string sourceFolder;
        private readonly string sourceFileWithoutExtension;
        private readonly string extension;
        private readonly bool outputDifferentFormat;
        private readonly string outputFormat;
        private readonly ObservableCollection<(TimeSpan, TimeSpan)> times;
        private readonly bool combineVideo;
        private readonly ProcessStartInfo startInfo;
        private readonly List<string> filenamesWithExtra = new List<string>();
        private readonly List<string> filenames = new List<string>();
        private string tempfile;
        private decimal percentage;
        private bool cancelled;
        private TimeSpan newDur = TimeSpan.Zero;
        private Process process;

        public VideoSplitter(string sFolder, string sFileWithoutExtension, string ext, ObservableCollection<(TimeSpan, TimeSpan)> t, bool combine, bool outputDiffFormat, string outFormat)
        {
            cancelled = false;
            finished = false;
            sourceFolder = sFolder;
            sourceFileWithoutExtension = sFileWithoutExtension;
            extension = ext;
            times = t;
            combineVideo = combine;
            outputDifferentFormat = outputDiffFormat;
            outputFormat = outFormat;

            var libsPath = "";
            var directoryName = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            if (directoryName != null)
                libsPath = Path.Combine(directoryName, "Binaries", "CSPlugins", "FFmpeg", IntPtr.Size == 8 ? "x64" : "x86");
            if (string.IsNullOrEmpty(libsPath))
                throw new Exception("Cannot read 'binaryfolder' variable from app.config / web.config.");

            var args = $"-y -i \"{sourceFolder}\\{sourceFileWithoutExtension}{extension}\"";
            var sb = new StringBuilder(args);

            for (int i = 0; i < times.Count; i++)
            {
                var output = $"{sourceFolder}\\{sourceFileWithoutExtension}_trimmed{i + 1}{(outputDifferentFormat ? outputFormat : extension)}";
                sb.Append($" -codec copy -ss {times[i].Item1.TotalSeconds} -to {times[i].Item2.TotalSeconds} \"{output}\"");

                filenames.Add(output);
                filenamesWithExtra.Add($"file '{output}'");

                newDur += times[i].Item2 - times[i].Item1;
            }
            // setup the process that will fire youtube-dl
            startInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = Path.Combine(libsPath, "ffmpeg.exe"),
                CreateNoWindow = true,
                Arguments = sb.ToString()
            };
        }

        public void Split()
        {
            try
            {
                OnDownloadStarted(new DownloadEventArgs());
                process = new Process();
                process.EnableRaisingEvents = true;
                process.Exited += Process_Exited;
                process.ErrorDataReceived += OutputHandler;

                process.StartInfo = startInfo;
                process.Start();
                process.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                OnDownloadError(new ProgressEventArgs { Error = ex.Message });
            }

            while (finished == false)
            {
                Thread.Sleep(100); // wait while process exits;
            }
        }

        public override void CancelOperation()
        {
            cancelled = true;
            if (!process.HasExited)
            {
                process.Kill();
                Thread.Sleep(100);
            }
            if (!string.IsNullOrEmpty(tempfile))
                File.Delete(tempfile);
            filenames.ForEach(f =>
            {
                if (File.Exists(f))
                    File.Delete(f);
            });
        }

        private void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (cancelled)
                return;

            OnProgress(new ProgressEventArgs { Percentage = finished ? 0 : percentage, Data = finished ? string.Empty : outLine.Data });
            // extract the percentage from process output
            if (string.IsNullOrEmpty(outLine.Data) || finished || isFinished())
            {
                OnDownloadFinished(new DownloadEventArgs());
                return;
            }

            if (outLine.Data.Contains("ERROR") || outLine.Data.Contains("Invalid"))
            {
                OnDownloadError(new ProgressEventArgs { Error = outLine.Data });
                return;
            }

            if (!isSplitting())
                return;

            var currentTime = TimeSpan.Zero;
            if (isSplitting())
            {
                var strSub = outLine.Data.Split(new[] { "time=" }, StringSplitOptions.RemoveEmptyEntries)[1].Substring(0, 11);
                currentTime = TimeSpan.Parse(strSub);
            }

            // fire the process event
            var perc = Convert.ToDecimal((float)currentTime.TotalSeconds / (float)newDur.TotalSeconds) * 100;
            if (perc < 0)
            {
                Console.WriteLine("weird perc {0}", perc);
                return;
            }
            percentage = perc;
            OnProgress(new ProgressEventArgs { Percentage = perc, Data = outLine.Data });

            // is it finished?
            if (perc < 100 && !isFinished())
                return;

            if (perc >= 100 && !finished)
                OnProgress(new ProgressEventArgs { Percentage = percentage, Data = outLine.Data });

            bool isSplitting() => outLine.Data.Contains("frame=") && outLine.Data.Contains("fps=") && outLine.Data.Contains("time=");
            bool isFinished() => outLine.Data.Contains("global headers:") && outLine.Data.Contains("muxing overhead:");
        }

        private void CleanUp()
        {
            if (!process.HasExited)
            {
                process.Close();
                Thread.Sleep(100);
            }
            if (!string.IsNullOrEmpty(tempfile))
                File.Delete(tempfile);
            if (combineVideo)
                filenames.ForEach(File.Delete);
        }

        private void CombineSections()
        {
            tempfile = Path.Combine(sourceFolder, $"temp_section_filenames{Guid.NewGuid()}.txt");
            File.WriteAllLines(tempfile, filenamesWithExtra);

            process = new Process();
            process.EnableRaisingEvents = true;
            process.Exited += CombineProcess_Exited;
            process.ErrorDataReceived += CombineProcess_ErrorDataReceived;

            startInfo.Arguments = $"-safe 0 -f concat -i \"{tempfile}\" -c copy \"{sourceFolder}\\{sourceFileWithoutExtension}_combined{(outputDifferentFormat ? outputFormat : extension)}\"";
            process.StartInfo = startInfo;
            process.Start();
            process.BeginErrorReadLine();
        }

        private void CombineProcess_ErrorDataReceived(object sender, DataReceivedEventArgs outline)
        {

        }

        private void CombineProcess_Exited(object sender, EventArgs e)
        {
            FinishedDownload?.Invoke(this, new DownloadEventArgs());
            CleanUp();
        }

        protected override void Process_Exited(object sender, EventArgs e)
        {
            if (finished)
                return;

            finished = true;
            if (combineVideo)
                CombineSections();
            else
            {
                FinishedDownload?.Invoke(this, new DownloadEventArgs());
                CleanUp();
            }
        }

        protected override void OnProgress(ProgressEventArgs e) => ProgressDownload?.Invoke(this, e);

        protected override void OnDownloadFinished(DownloadEventArgs e)
        {

        }

        protected override void OnDownloadStarted(DownloadEventArgs e) => StartedDownload?.Invoke(this, e);

        protected override void OnDownloadError(ProgressEventArgs e)
        {
            ErrorDownload?.Invoke(this, e);
            CleanUp();
        }

        protected override void ErrorDataReceived(object sendingprocess, DataReceivedEventArgs error)
        {
            if (!string.IsNullOrEmpty(error.Data))
                OnDownloadError(new ProgressEventArgs { Error = error.Data });
        }
    }
}
