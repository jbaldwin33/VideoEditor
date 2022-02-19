using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VideoUtilities
{
    public class VideoConverter : BaseClass
    {
        private readonly string outExtension;
        
        public VideoConverter(ConverterArgs args) : base(args.InputFiles)
        {
            OutputPath = $"{args.OutputPath}\\{Path.GetFileNameWithoutExtension(args.InputFiles.First().FilePath)}_converted{args.OutputFormat}";
            outExtension = args.OutputFormat;
        }
        
        public override void Setup() => DoSetup(null);

        protected override string CreateOutput(int index, object obj)
        {
            var args = (ConverterPathClass)obj;
            return $"{Path.GetDirectoryName(OutputPath)}\\{Path.GetFileNameWithoutExtension(args.FilePath)}_converted{outExtension}";
        }

        protected override string CreateArguments(int index, ref string output, object obj)
        {
            var args = (ConverterPathClass)obj;
            var copyClause = (Path.GetExtension(args.FilePath) == ".webm" && outExtension == ".mp4") || outExtension == ".avi" ? string.Empty : "-c:v copy";
            var qScaleClause = outExtension == ".avi" ? "-q:v 0 -q:a 0" : string.Empty;
            var subPath = args.FilePath.Replace(@"\", @"\\\\").Replace(":", @"\\:");
            var subtitleClause = args.EmbedSubs ? $"-vf subtitles=\"{subPath}\"" : string.Empty;
            return $"{(CheckOverwrite(ref output) ? "-y" : string.Empty)} -i \"{args.FilePath}\" -c:a copy {copyClause} {subtitleClause} {qScaleClause} \"{output}\"";
        }

        protected override TimeSpan? GetDuration(object obj) => null;

        protected override void CleanUp()
        {
            
        }
    }
}
