using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VideoUtilities
{
    public class VideoDownloader : BaseClass
    {
        private readonly bool extractAudio;

        public VideoDownloader(DownloaderArgs args) : base(args.Urls)
        {
            UseYoutubeDL = true;
            IsList.AddRange(args.Urls.Select(u => u.IsPlaylist));
            extractAudio = args.ExtractAudio;
            OutputPath = args.OutputPath;
        }

        protected override string CreateOutput(int index, object obj) => Path.Combine(OutputPath, $"%(title)s.%(ext)s");

        protected override string CreateArguments(int index, ref string output, object obj)
        {
            var args = (UrlClass)obj;
            return args.IsPlaylist
                ? string.Format($"--continue --no-overwrites --restrict-filenames {(extractAudio ? "--extract-audio --audio-format mp3" : string.Empty)} --no-part -f best --playlist-start 1 --yes-playlist --add-metadata \"{args.Url}\" -o {output}")
                : string.Format($"--continue --no-overwrites --restrict-filenames {(extractAudio ? "--extract-audio --audio-format mp3" : string.Empty)} --no-part -f best --add-metadata {args.Url} -o \"{output}\"");
        }

        protected override TimeSpan? GetDuration(object obj) => null;
        public override void Setup() => DoSetup(null);

        protected override void CleanUp()
        {

        }
    }
}
