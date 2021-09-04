using System;
using System.Collections.Generic;
using System.IO;

namespace VideoUtilities
{
    public class VideoDownloader : BaseClass<string>
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

        protected override string CreateOutput(string obj, int index)
        {
            return /*IsList
                ? */Path.Combine(destinationFolder, $"%(title)s.{extension}");
            //: Path.Combine(destinationFolder, $"{outputFolder}.{extension}");
        }

        protected override string CreateArguments(string obj, int index, ref string output)
        {
            return IsList
                ? string.Format($"--continue  --no-overwrites --restrict-filenames --no-part --playlist-start 1 --yes-playlist \"{obj}\" -o {output}")
                : string.Format($"--continue  --no-overwrites --restrict-filenames --no-part -f best --add-metadata {obj} -o \"{output}\"");
        }

        protected override TimeSpan? GetDuration(string obj) => null;
        public override void Setup() => DoSetup(null);

        protected override void CleanUp()
        {
            
        }
    }
}
