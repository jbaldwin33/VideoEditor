using System;
using System.Diagnostics;
using System.IO;

namespace VideoUtilities
{
    public class VideoSpeedChanger : BaseClass<string>
    {
        private readonly Enums.ScaleRotate scaleRotate;
        private readonly double newSpeed;

        public VideoSpeedChanger(string fullPath, double speed, Enums.ScaleRotate sr)
        {
            Failed = false;
            Cancelled = false;
            scaleRotate = sr;
            newSpeed = speed;
            SetList(new[] { fullPath });
        }

        public override void Setup() => DoSetup(null);
        protected override string CreateOutput(string obj, int index)
            => $"{Path.GetDirectoryName(obj)}\\{Path.GetFileNameWithoutExtension(obj)}_formatted{Path.GetExtension(obj)}";

        protected override string CreateArguments(string obj, int index, ref string output)
        {
            var filter = string.Empty;
            var overwrite = false;
            switch (scaleRotate)
            {
                case Enums.ScaleRotate.NoSNoR: break;
                case Enums.ScaleRotate.NoS90R: filter = ",transpose=1"; break;
                case Enums.ScaleRotate.NoS180R: filter = ",vflip,hflip"; break;
                case Enums.ScaleRotate.NoS270R: filter = ",transpose=2"; break;
                case Enums.ScaleRotate.SNoR: filter = ",hflip"; break;
                case Enums.ScaleRotate.S90R: filter = ",hflip,transpose=1"; break;
                case Enums.ScaleRotate.S180R: filter = ",vflip"; break;
                case Enums.ScaleRotate.S270R: filter = ",hflip,transpose=2"; break;
                default: throw new ArgumentOutOfRangeException(nameof(scaleRotate), scaleRotate, null);
            }

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

            return $"{(overwrite ? "-y" : string.Empty)} -i \"{obj}\" -filter_complex \"[0:v]setpts={1 / newSpeed}*PTS{filter}[v];[0:a]atempo={newSpeed}[a]\" -map \"[v]\" -map \"[a]\" \"{output}\"";
        }

        protected override TimeSpan? GetDuration(string obj) => null;
        protected override void CleanUp()
        {
            
        }
    }
}