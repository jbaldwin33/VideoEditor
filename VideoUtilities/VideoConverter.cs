using System;
using System.Collections.Generic;
using System.IO;

namespace VideoUtilities
{
    public class VideoConverter : BaseClass<(string Folder, string Filename, string Extension)>
    {
        private readonly string outExtension;

        public VideoConverter(IEnumerable<(string folder, string filename, string extension)> fileViewModels, string outExt)
        {
            Failed = false;
            Cancelled = false;
            outExtension = outExt;
            SetList(fileViewModels);
        }

        public void Setup() => DoSetup(null);

        protected override string CreateOutput((string Folder, string Filename, string Extension) obj, int index) 
            => $"{obj.Folder}\\{obj.Filename}_converted{outExtension}";

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
            return $"{(overwrite ? "-y" : string.Empty)} -i \"{obj.Folder}\\{obj.Filename}{obj.Extension}\" -c:a copy -c:v copy \"{output}\"";
        }

        protected override TimeSpan? GetDuration((string Folder, string Filename, string Extension) obj) => null;
        
        public override void CancelOperation(string cancelMessage)
        {
            base.CancelOperation(cancelMessage);
            OnDownloadFinished(new FinishedEventArgs { Cancelled = Cancelled, Message = cancelMessage});
        }

        protected override void CleanUp()
        {
            
        }
    }
}
