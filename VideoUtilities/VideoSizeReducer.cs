using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VideoUtilities
{
    public class VideoSizeReducer : BaseClass
    {
        public VideoSizeReducer(ReducerArgs args) : base(args.InputPaths)
        {
            OutputPath = $"{args.OutputPath}\\{Path.GetFileNameWithoutExtension(args.InputPaths.First())}_reduced{Path.GetExtension(args.InputPaths.First())}";
        }

        public override void Setup() => DoSetup(null);

        protected override string CreateOutput(int index, object obj)
        {
            var args = (string)obj;
            return $"{Path.GetDirectoryName(OutputPath)}\\{Path.GetFileNameWithoutExtension(args)}_reduced{Path.GetExtension(args)}";
        }

        protected override string CreateArguments(int index, ref string output, object obj)
        {
            var args = (string)obj;
            return $"{(CheckOverwrite(ref output) ? "-y" : string.Empty)} -i \"{Path.GetDirectoryName(args)}\\{Path.GetFileNameWithoutExtension(args)}{Path.GetExtension(args)}\" -vcodec libx264 -crf 28 \"{output}\"";
        }

        protected override TimeSpan? GetDuration(object obj) => null;

        protected override void CleanUp()
        {
            
        }
    }
}
