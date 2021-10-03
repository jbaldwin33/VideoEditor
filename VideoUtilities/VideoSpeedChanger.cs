using System;
using System.IO;

namespace VideoUtilities
{
    public class VideoSpeedChanger : BaseClass
    {
        private readonly Enums.ScaleRotate scaleRotate;
        private readonly double newSpeed;

        public VideoSpeedChanger(string fullPath, double speed, Enums.ScaleRotate sr)
        {
            Failed = false;
            Cancelled = false;
            OutputPath = $"{Path.GetDirectoryName(fullPath)}\\{Path.GetFileNameWithoutExtension(fullPath)}_formatted{Path.GetExtension(fullPath)}";
            scaleRotate = sr;
            newSpeed = speed;
            SetList(new[] { fullPath });
        }

        public override void Setup() => DoSetup(null);
        protected override string CreateOutput(int index, object obj)
            => $"{Path.GetDirectoryName((string)obj)}\\{Path.GetFileNameWithoutExtension((string)obj)}_formatted{Path.GetExtension((string)obj)}";

        protected override string CreateArguments(int index, ref string output, object obj)
        {
            obj = obj as string;
            var filter = string.Empty;
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

            return $"{(CheckOverwrite(ref output) ? "-y" : string.Empty)} -i \"{obj}\" -filter_complex \"[0:v]setpts={1 / newSpeed}*PTS{filter}[v];[0:a]atempo={newSpeed}[a]\" -map \"[v]\" -map \"[a]\" \"{output}\"";
        }

        protected override TimeSpan? GetDuration(object obj) => null;
        protected override void CleanUp()
        {
            
        }
    }
}