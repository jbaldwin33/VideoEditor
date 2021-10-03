using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VideoUtilities
{
    public class VideoSizeReducer : BaseClass
    {
        public VideoSizeReducer(IEnumerable<(string Folder, string Filename, string Extension)> fileViewModels, string outPath)
        {
            Failed = false;
            Cancelled = false;
            OutputPath = $"{outPath}\\{fileViewModels.First().Filename}_reduced{fileViewModels.First().Extension}";
            SetList(fileViewModels);
        }

        public override void Setup() => DoSetup(null);

        protected override string CreateOutput(int index, object obj)
        {
            var(_, filename, extension) = (ValueTuple<string, string, string>)obj;
            return $"{Path.GetDirectoryName(OutputPath)}\\{filename}_reduced{extension}";
        }

        protected override string CreateArguments(int index, ref string output, object obj)
        {
            var (folder, filename, extension) = (ValueTuple<string, string, string>)obj;
            return $"{(CheckOverwrite(ref output) ? "-y" : string.Empty)} -i \"{folder}\\{filename}{extension}\" -vcodec libx264 -crf 28 \"{output}\"";
        }

        protected override TimeSpan? GetDuration(object obj) => null;

        protected override void CleanUp()
        {
            
        }
    }
}
