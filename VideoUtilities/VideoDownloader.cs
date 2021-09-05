using System;
using System.Collections.Generic;
using System.IO;

namespace VideoUtilities
{
    public class VideoDownloader : BaseClass
    {
        private readonly string extension;
        private readonly string destinationFolder;

        public VideoDownloader(List<string> urls, string output, string ext, bool isList = false)
        {
            UseYoutubeDL = true;
            IsList = isList;
            destinationFolder = output;
            extension = ext;
            SetList(urls);
        }

        protected override string CreateOutput(int index, object obj) => Path.Combine(destinationFolder, $"%(title)s.{extension}");

        protected override string CreateArguments(int index, ref string output, object obj)
        {
            return IsList
                ? string.Format($"--continue  --no-overwrites --restrict-filenames --no-part --playlist-start 1 --yes-playlist \"{obj}\" -o {output}")
                : string.Format($"--continue  --no-overwrites --restrict-filenames --no-part -f best --add-metadata {obj} -o \"{output}\"");
        }

        protected override TimeSpan? GetDuration(object obj) => null;
        public override void Setup() => DoSetup(null);

        protected override void CleanUp()
        {

        }
    }
}
