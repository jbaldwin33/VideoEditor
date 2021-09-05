using System;
using System.Collections.Generic;
using System.IO;

namespace VideoUtilities
{
    public class VideoSizeReducer : BaseClass//<(string Folder, string Filename, string Extension)>
    {
        private readonly string outputPath;

        public VideoSizeReducer(IEnumerable<(string Folder, string Filename, string Extension)> fileViewModels, string outPath)
        {
            Failed = false;
            Cancelled = false;
            outputPath = outPath;
            SetList(fileViewModels);
        }

        public override void Setup() => DoSetup(null);

        protected override string CreateOutput(int index, object obj)
        {
            var(_, filename, extension) = (ValueTuple<TimeSpan, TimeSpan, string>)obj;
            return $"{outputPath}\\{filename}_reduced{extension}";
        }

        protected override string CreateArguments(int index, ref string output, object obj)
        {
            var (folder, filename, extension) = (ValueTuple<TimeSpan, TimeSpan, string>)obj;
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
            return $"{(overwrite ? "-y" : string.Empty)} -i \"{folder}\\{filename}{extension}\" -vcodec libx264 -crf 28 \"{output}\"";
        }

        protected override TimeSpan? GetDuration(object obj) => null;

        protected override void CleanUp()
        {
            
        }
    }
}
