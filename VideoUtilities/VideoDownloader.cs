using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VideoUtilities
{
    public class VideoDownloader : BaseClass<string>
    {
        
        public bool Started { get; set; }

        public string OutputName { get; set; }
        public string DestinationFolder { get; set; }
        public int ToDownload { get; set; }
        public int Downloaded { get; set; }
        public bool IsList { get; set; }

        public VideoDownloader(List<string> urls, string outputName, string outputfolder, bool isList = false)
        {
            UseYoutubeDL = true;
            Started = false;
            IsList = isList;
            DestinationFolder = outputfolder;
            OutputName = outputName;

            var destinationPath = Path.Combine(outputfolder, $"{OutputName}.%(ext)s");
            var playlistDir = Path.Combine(outputfolder, $"%(title)s.%(ext)s");
            SetList(urls);
        }

        protected override string CreateOutput(string obj, int index)
        {
            return IsList
                ? Path.Combine(DestinationFolder, "%(title)s.%(ext)s")
                : Path.Combine(DestinationFolder, $"{OutputName}.%(ext)s");
        }

        protected override string CreateArguments(string obj, int index, ref string output)
        {
            return IsList
                ? string.Format($"--continue  --no-overwrites --restrict-filenames --playlist-start 1 --yes-playlist \"{obj}\" -o {output}")
                : string.Format($"--continue  --no-overwrites --restrict-filenames -f best --add-metadata {obj} -o \"{output}\"");
        }

        protected override TimeSpan? GetDuration(string obj) => null;
        protected override void CleanUp()
        {
            base.CleanUp();
        }

        protected override void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            var index = ProcessStuff.FindIndex(p => p.Process.Id == (sendingProcess as Process).Id);
            OnProgress(new ProgressEventArgs { Percentage = ProcessStuff[index].Finished ? 0 : ProcessStuff[index].Percentage, Data = ProcessStuff[index].Finished ? string.Empty : outLine.Data });
            // extract the percentage from process output
            if (string.IsNullOrEmpty(outLine.Data) || ProcessStuff[index].Finished)
                return;

            if (outLine.Data.Contains("Finished downloading playlist"))
                Downloaded++;

            if (outLine.Data.Contains("ERROR"))
            {
                OnDownloadError(new ProgressEventArgs { Error = outLine.Data});
                return;
            }

            if (!outLine.Data.Contains("[download]"))
                return;

            if (outLine.Data.Contains("Downloading video ") && outLine.Data.Contains(" of "))
            {
                var str = outLine.Data.Substring(outLine.Data.IndexOf("video "));
                var b = string.Empty;
                var c = string.Empty;
                for (int i = 0; i < str.Length; i++)
                {
                    if (char.IsDigit(str[i]))
                        if (string.IsNullOrEmpty(b))
                            b += str[i];
                        else
                            c += str[i];
                }

                if (b.Length > 0)
                    Downloaded = int.Parse(b) - 1;
                if (c.Length > 0)
                    ToDownload = int.Parse(c);
            }

            if (Downloaded > ToDownload)
            {
                OnDownloadError(new ProgressEventArgs { Error = "This playlist is empty. No videos were downloaded"});
                return;
            }

            var pattern = new Regex(@"\b\d+([\.,]\d+)?", RegexOptions.None);
            if (!pattern.IsMatch(outLine.Data) && ((Downloaded == 0 && ToDownload == 0) || (ToDownload != 0 && Downloaded != ToDownload)))
            {
                return;
            }

            // fire the process event
            var perc = IsList
              ? Convert.ToDecimal(Downloaded / ToDownload * 100)
              : Convert.ToDecimal(Regex.Match(outLine.Data, @"\b\d+([\.,]\d+)?").Value, System.Globalization.CultureInfo.InvariantCulture);

            if (perc > 100 || perc < 0)
            {
                Console.WriteLine("weird perc {0}", perc);
                return;
            }
            OnProgress(new ProgressEventArgs { Percentage = perc, Data = outLine.Data });

            // is it finished?
            if (perc < 100)
            {
                return;
            }

            if (perc == 100 && !ProcessStuff[index].Finished && ToDownload == Downloaded)
                OnDownloadFinished(new FinishedEventArgs { Cancelled = Cancelled, ProcessIndex = index});
        }

    }
}
