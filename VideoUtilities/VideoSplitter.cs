using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using static VideoUtilities.Enums.Enums;

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
        private readonly List<string> arguments = new List<string>();
        private readonly List<string> filenamesWithExtra = new List<string>();
        private readonly List<string> filenames = new List<string>();
        private string tempfile;
        private List<Process> processes = new List<Process>();
        private decimal percentage;
        private int toSplit;
        private static int currentSplit;
        private static object _lock = new object();

        public VideoSplitter(string sFolder, string sFileWithoutExtension, string ext, ObservableCollection<(TimeSpan, TimeSpan)> t, bool combine, bool outputDiffFormat, string outFormat)
        {
            finished = false;
            sourceFolder = sFolder;
            sourceFileWithoutExtension = sFileWithoutExtension;
            extension = ext;
            times = t;
            combineVideo = combine;
            outputDifferentFormat = outputDiffFormat;
            outputFormat = outFormat;
            toSplit = t.Count;

            var libsPath = "";
            var directoryName = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            if (directoryName != null)
                libsPath = Path.Combine(directoryName, "Binaries", "CSPlugins", "FFmpeg", IntPtr.Size == 8 ? "x64" : "x86");
            if (string.IsNullOrEmpty(libsPath))
                throw new Exception("Cannot read 'binaryfolder' variable from app.config / web.config.");

            for (int i = 0; i < times.Count; i++)
            {
                var output = $"{sourceFolder}\\{sourceFileWithoutExtension}_trimmed{i + 1}{(outputDifferentFormat ? outputFormat : extension)}";
                arguments.Add($"-y -i {sourceFolder}\\{sourceFileWithoutExtension}{extension} -ss {times[i].Item1.TotalSeconds} -t {times[i].Item2.TotalSeconds} -c copy {output}");
                filenames.Add(output);
                filenamesWithExtra.Add($"file '{output}'");
            }

            // setup the process that will fire youtube-dl
            startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.FileName = Path.Combine(libsPath, "ffmpeg.exe");
            startInfo.CreateNoWindow = true;
        }

        public void Split()
        {
            processes.Clear();
            try
            {
                OnDownloadStarted(new DownloadEventArgs());
                for (int i = 0; i < arguments.Count; i++)
                {
                    var process = new Process();
                    process.EnableRaisingEvents = true;
                    process.Exited += Process_Exited;
                    process.ErrorDataReceived += new DataReceivedEventHandler(OutputHandler);

                    startInfo.Arguments = arguments[i];
                    process.StartInfo = startInfo;
                    process.Start();
                    process.BeginErrorReadLine();
                }
            }
            catch (Exception ex)
            {
                OnDownloadError(new ProgressEventArgs { Error = ex.Message });
            }

            while (finished == false)
            {
                System.Threading.Thread.Sleep(100); // wait while process exits;
            }
        }

        private void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            var str = (sendingProcess as Process).StartInfo.Arguments.Split(new string[] { "-ss " }, StringSplitOptions.None)[1];
            var startSec = double.Parse(str.Substring(0, 7));
            var endSec = double.Parse(str.Split(new string[] { "-t " }, StringSplitOptions.None)[1].Substring(0, 7));
            var dur = TimeSpan.FromSeconds(endSec - startSec);
            OnProgress(new ProgressEventArgs() { Percentage = finished ? 0 : percentage, Data = finished ? string.Empty : outLine.Data });
            // extract the percentage from process outpu
            if (string.IsNullOrEmpty(outLine.Data) || finished || isFinished())
            {
                OnDownloadFinished(new DownloadEventArgs());
                return;
            }

            if (outLine.Data.Contains("ERROR"))
            {
                OnDownloadError(new ProgressEventArgs() { Error = outLine.Data });
                return;
            }

            if (!isSplitting())
                return;

            TimeSpan currentTime = TimeSpan.Zero;
            if (isSplitting())
            {
                var strSub = outLine.Data.Split(new string[] { "time=" }, StringSplitOptions.RemoveEmptyEntries)[1].Substring(0, 11);
                currentTime = TimeSpan.Parse(strSub);
            }

            // fire the process event
            var perc = Convert.ToDecimal((float)currentTime.TotalSeconds / (float)dur.TotalSeconds) * 100;
            if (perc < 0)
            {
                Console.WriteLine("weird perc {0}", perc);
                return;
            }
            percentage = perc;
            OnProgress(new ProgressEventArgs() { Percentage = perc, Data = outLine.Data });

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
            processes.ForEach(p =>
            {
                if (!p.HasExited)
                    p.Close();
            });
            if (!string.IsNullOrEmpty(tempfile))
                File.Delete(tempfile);
            if (combineVideo)
                filenames.ForEach(file => File.Delete(file));
        }

        private void CombineSections()
        {
            tempfile = Path.Combine(sourceFolder, $"temp_section_filenames{Guid.NewGuid()}.txt");
            File.WriteAllLines(tempfile, filenamesWithExtra);

            var process = new Process();
            process.EnableRaisingEvents = true;
            process.Exited += CombineProcess_Exited;
            //process.ErrorDataReceived += new DataReceivedEventHandler(ErrorDataReceived);

            startInfo.Arguments = $"-safe 0 -f concat -i {tempfile} -c copy {sourceFolder}\\{sourceFileWithoutExtension}_combined{(outputDifferentFormat ? outputFormat : extension)}";
            process.StartInfo = startInfo;
            process.Start();
            //process.BeginErrorReadLine();
        }

        private void CombineProcess_Exited(object sender, EventArgs e)
        {
            FinishedDownload?.Invoke(this, new DownloadEventArgs());
            CleanUp();
        }

        protected override void Process_Exited(object sender, EventArgs e)
        {
            lock (_lock)
            {
                currentSplit++;
                if (!finished && toSplit == currentSplit)
                {
                    finished = true;
                    if (combineVideo)
                        CombineSections();
                    else
                    {
                        FinishedDownload?.Invoke(this, new DownloadEventArgs());
                        CleanUp();
                    }
                }
            }
        }

        protected virtual void OnProgress(ProgressEventArgs e)
        {
            if (ProgressDownload != null)
                ProgressDownload(this, e);
        }

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
                OnDownloadError(new ProgressEventArgs() { Error = error.Data });
        }
    }
}
