using System;
using System.Collections.Generic;
using System.IO;

namespace VideoUtilities
{
    public class VideoSizeReducer : BaseClass<(string Folder, string Filename, string Extension)>
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

        protected override string CreateOutput((string Folder, string Filename, string Extension) obj, int index) => $"{outputPath}\\{obj.Filename}_reduced{obj.Extension}";

        protected override string CreateArguments((string Folder, string Filename, string Extension) obj, int index, ref string output)
        {
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
                    var filename = Path.GetFileNameWithoutExtension(output);
                    output = $"{Path.GetDirectoryName(output)}\\{filename}[0]{Path.GetExtension(output)}";
                }
            }
            return $"{(overwrite ? "-y" : string.Empty)} -i \"{obj.Folder}\\{obj.Filename}{obj.Extension}\" -vcodec libx264 -crf 28 \"{output}\"";
        }

        protected override TimeSpan? GetDuration((string Folder, string Filename, string Extension) obj) => null;

        public override void CancelOperation(string cancelMessage)
        {
            base.CancelOperation(cancelMessage);
            OnDownloadFinished(new FinishedEventArgs { Cancelled = Cancelled, Message = cancelMessage });
        }

        protected override void CleanUp()
        {
            
        }
    }
}
