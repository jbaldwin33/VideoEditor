using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VideoUtilities
{
    public class VideoDownloader : BaseClass
    {
        private readonly bool extractAudio;

        public VideoDownloader(IEnumerable<(string, bool)> urls, bool justAudio, string output)
        {
            UseYoutubeDL = true;
            IsList.AddRange(urls.Select(u => u.Item2));
            extractAudio = justAudio;
            OutputPath = output;
            SetList(urls);
        }

        protected override string CreateOutput(int index, object obj) => Path.Combine(OutputPath, $"%(title)s.%(ext)s");

        protected override string CreateArguments(int index, ref string output, object obj)
        {
            var (url, isList) = (ValueTuple<string, bool>)obj;
            return isList 
                ? string.Format($"--continue --no-overwrites --restrict-filenames {(extractAudio ? "--extract-audio --audio-format mp3" : string.Empty)} --no-part -f best --playlist-start 1 --yes-playlist --add-metadata \"{url}\" -o {output}")
                : string.Format($"--continue --no-overwrites --restrict-filenames {(extractAudio ? "--extract-audio --audio-format mp3" : string.Empty)} --no-part -f best --add-metadata {url} -o \"{output}\"");
        }

        protected override TimeSpan? GetDuration(object obj) => null;
        public override void Setup() => DoSetup(null);

        protected override void CleanUp()
        {

        }
    }
}
