using System;
using System.Collections.Generic;
using System.IO;

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
            var overwrite = false;
            if (File.Exists(output))
            {
                var args = new MessageEventArgs
                {
                    Message = $"The file {Path.GetFileName(output)} already exists. Overwrite? (Select \"No\" to output to a different file name.)"
                };
                ShowMessage(args);
                overwrite = args.Result;
                if (!overwrite)
                {
                    var filename2 = Path.GetFileNameWithoutExtension(output);
                    output = $"{Path.GetDirectoryName(output)}\\{filename2}[0]{Path.GetExtension(output)}";
                }
            }
            return $"{(overwrite ? "-y" : string.Empty)} -i \"{folder}\\{filename}{extension}\" -c:a copy -c:v copy \"{output}\"";
        }

        protected override TimeSpan? GetDuration(object obj) => null;

        protected override void CleanUp()
        {
            
        }
    }
}
