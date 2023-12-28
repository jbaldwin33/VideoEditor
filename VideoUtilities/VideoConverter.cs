using System;
using System.IO;
using System.Linq;

namespace VideoUtilities
{
    public class VideoConverter : BaseClass
    {
        private readonly string outExtension;
        
        public VideoConverter(ConverterArgs args) : base(args.InputFiles)
        {
            Args = args;
            OutputPath = $"{args.OutputPath}\\{Path.GetFileNameWithoutExtension(args.InputFiles.First().FilePath)}_converted{args.OutputFormat}";
            outExtension = args.OutputFormat;
        }
        
        public override void Setup() => DoSetup(null);

        public override void DoPreCheck(out bool isError)
        {
            isError = false;
            var converterArgs = Args as ConverterArgs;
            if (converterArgs.InputFiles.Any(x => Path.GetDirectoryName(x.FilePath) != Path.GetDirectoryName(converterArgs.InputFiles.First().FilePath)))
            {
                //TODO make translatable
                var messageArgs = new MessageEventArgs
                {
                    Message = "Please make sure all input files are in the same folder."
                };
                ShowMessage(messageArgs);
                isError = true;
            }
        }

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
            // subtitle filter is a literal string so backslash needs multiple escapes
            var subPath = args.FilePath.Replace(@"\", @"\\\\").Replace(":", @"\\:").Replace("[", "\\[").Replace("]", "\\]");
            var subtitleClause = args.EmbedSubs ? $"-vf \"subtitles={subPath}\"" : string.Empty;

            return $"{(CheckOverwrite(ref output) ? "-y" : string.Empty)} -i \"{args.FilePath}\" -c:a copy {copyClause} {subtitleClause} {qScaleClause} \"{output}\"";
        }

        protected override TimeSpan? GetDuration(object obj) => null;

        protected override void CleanUp()
        {
            
        }
    }
}
