﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Threading;

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
        private readonly ObservableCollection<(TimeSpan, TimeSpan)> times;
        private readonly bool combineVideo;
        private readonly ProcessStartInfo startInfo;
        private readonly List<string> arguments = new List<string>();
        private readonly List<string> filenamesWithExtra = new List<string>();
        private readonly List<string> filenames = new List<string>();
        private string tempfile;
        private TimeSpan duration = TimeSpan.Zero;
        private List<Process> processes = new List<Process>();
        private decimal percentage;

        public VideoSplitter(string sourceFolder, string sourceFileWithoutExtension, string extension, ObservableCollection<(TimeSpan, TimeSpan)> times, bool combineVideo)
        {
            finished = false;
            this.sourceFolder = sourceFolder;
            this.sourceFileWithoutExtension = sourceFileWithoutExtension;
            this.extension = extension;
            this.times = times;
            this.combineVideo = combineVideo;

            //var binaryPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Binaries");

            var libsPath = "";
            string directoryName = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            if (directoryName != null)
                libsPath = Path.Combine(directoryName, "Binaries", "CSPlugins", "FFmpeg", IntPtr.Size == 8 ? "x64" : "x86");
            if (string.IsNullOrEmpty(libsPath))
                throw new Exception("Cannot read 'binaryfolder' variable from app.config / web.config.");

            for (int i = 0; i < this.times.Count; i++)
            {
                var output = $"{this.sourceFolder}\\{this.sourceFileWithoutExtension}_trimmed{i + 1}{this.extension}";
                arguments.Add(string.Format($"-i {this.sourceFolder}\\{this.sourceFileWithoutExtension}{this.extension} -ss {this.times[i].Item1.TotalSeconds} -t {this.times[i].Item2.TotalSeconds} -c copy {output}"));
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

        public void Download()
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
                    //process.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
                    process.ErrorDataReceived += new DataReceivedEventHandler(OutputHandler);

                    startInfo.Arguments = arguments[i];
                    process.StartInfo = startInfo;
                    process.Start();
                    process.BeginErrorReadLine();
                    //process.WaitForExit();
                    //processes.Add(process);
                    //var perc = Convert.ToDecimal((float)(i + 1) / (float)arguments.Count * 100);
                    //OnProgress(new ProgressEventArgs { Percentage = perc });
                }
                //if (combineVideo)
                //    CombineSections();
                //else
                //    finished = true;

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

            if (!outLine.Data.Contains("Duration: ") && !isSplitting())
                return;

            if (outLine.Data.Contains("Duration: "))
            {
                var str = outLine.Data.Split(',')[0].Split(new string[] { "Duration: " }, StringSplitOptions.RemoveEmptyEntries)[1];
                duration = TimeSpan.Parse(str);
                return;
            }

            if (!isSplitting())
                return;

            TimeSpan currentTime = TimeSpan.Zero;
            if (isSplitting())
            {
                var str = outLine.Data.Split(new string[] { "time=" }, StringSplitOptions.RemoveEmptyEntries)[1].Substring(0, 11);
                currentTime = TimeSpan.Parse(str);


            }

            //if (Downloaded > ToDownload)
            //{
            //    OnDownloadError(new ProgressEventArgs() { Error = "This playlist is empty. No videos were downloaded", ProcessObject = ProcessObject });
            //    return;
            //}

            var pattern = new Regex(@"\b\d+([\.,]\d+)?", RegexOptions.None);
            //if (!pattern.IsMatch(outLine.Data) && ((Downloaded == 0 && ToDownload == 0) || (ToDownload != 0 && Downloaded != ToDownload)))
            //{
            //    return;
            //}

            // fire the process event
            //var perc = Convert.ToDecimal(Regex.Match(outLine.Data, @"\b\d+([\.,]\d+)?").Value, System.Globalization.CultureInfo.InvariantCulture);
            var perc = Convert.ToDecimal((float)currentTime.TotalMilliseconds / (float)duration.TotalMilliseconds);
            if (perc > 100 || perc < 0)
            {
                Console.WriteLine("weird perc {0}", perc);
                return;
            }
            percentage = perc;
            OnProgress(new ProgressEventArgs() { Percentage = perc, Data = outLine.Data });

            // is it finished?
            if (perc < 100 && !isFinished())
                return;

            //if (perc >= 100 && !finished /*&& ToDownload == Downloaded*/)
            //{
            //    OnDownloadFinished(new DownloadEventArgs());
            //}

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
            process.Exited += Process_Exited;
            process.ErrorDataReceived += new DataReceivedEventHandler(ErrorDataReceived);

            startInfo.Arguments = $"-safe 0 -f concat -i {tempfile} -c copy {sourceFolder}\\{sourceFileWithoutExtension}_combined{extension}";
            process.StartInfo = startInfo;
            process.Start();
            processes.Add(process);
            finished = true;
        }

        protected override void Process_Exited(object sender, EventArgs e) { }//=> OnDownloadFinished(new DownloadEventArgs());

        protected virtual void OnProgress(ProgressEventArgs e)
        {
            if (ProgressDownload != null)
                ProgressDownload(this, e);
        }

        protected override void OnDownloadFinished(DownloadEventArgs e)
        {
            if (!finished)
            {
                finished = true;
                FinishedDownload?.Invoke(this, e);
                CleanUp();
            }
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
