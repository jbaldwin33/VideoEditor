using System;
using System.Collections.Generic;

namespace VideoUtilities
{
    public class VideoConverter : BaseClass
    {
        private readonly string outExtension;

        public VideoConverter(IEnumerable<(string folder, string filename, string extension)> fileViewModels, string outExt)
        {
            Failed = false;
            Cancelled = false;
            outExtension = outExt;
            SetList(fileViewModels);
        }

        public override void Setup() => DoSetup(null);

        protected override string CreateOutput(int index, object obj)
        {
            var (folder, filename, _) = (ValueTuple<string, string, string>)obj;
            return $"{folder}\\{filename}_converted{outExtension}";
        }

        protected override string CreateArguments(int index, ref string output, object obj)
        {
            var (folder, filename, extension) = (ValueTuple<string, string, string>)obj;
            var copyClause = extension == ".webm" && outExtension == ".mp4" ? string.Empty : "-c:a copy -c:v copy";
            return $"{(CheckOverwrite(ref output) ? "-y" : string.Empty)} -i \"{folder}\\{filename}{extension}\" {copyClause} \"{output}\"";
        }

        protected override TimeSpan? GetDuration(object obj) => null;

        protected override void CleanUp()
        {
            
        }
    }
}
