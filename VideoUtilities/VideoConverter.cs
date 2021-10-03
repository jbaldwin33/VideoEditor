using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VideoUtilities
{
    public class VideoConverter : BaseClass
    {
        private readonly string outExtension;

        public VideoConverter(IEnumerable<(string folder, string filename, string extension)> fileViewModels, string outExt, string outPath)
        {
            Failed = false;
            Cancelled = false;
            OutputPath = $"{outPath}\\{fileViewModels.First().filename}_converted{outExt}";
            outExtension = outExt;
            SetList(fileViewModels);
        }

        public override void Setup() => DoSetup(null);

        protected override string CreateOutput(int index, object obj)
        {
            var (_, filename, _) = (ValueTuple<string, string, string>)obj;
            return $"{Path.GetDirectoryName(OutputPath)}\\{filename}_converted{outExtension}";
        }

        protected override string CreateArguments(int index, ref string output, object obj)
        {
            var (folder, filename, extension) = (ValueTuple<string, string, string>)obj;
            var copyClause = (extension == ".webm" && outExtension == ".mp4") || outExtension == ".avi" ? string.Empty : "-c:v copy";
            return $"{(CheckOverwrite(ref output) ? "-y" : string.Empty)} -i \"{folder}\\{filename}{extension}\" -c:a copy {copyClause} \"{output}\"";
        }

        protected override TimeSpan? GetDuration(object obj) => null;

        protected override void CleanUp()
        {
            
        }
    }
}
