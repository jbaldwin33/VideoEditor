using System;
using System.IO;

namespace VideoUtilities
{
    public class VideoCropper : BaseClass
    {
        private readonly double width;
        private readonly double height;
        private readonly double xPos;
        private readonly double yPos;

        public VideoCropper(VideoCropperArgs args) : base(args.InputPaths)
        {
            OutputPath = $"{Path.GetDirectoryName(args.InputPaths[0])}\\{Path.GetFileNameWithoutExtension(args.InputPaths[0])}_formatted{Path.GetExtension(args.InputPaths[0])}";
            width = args.Width;
            height = args.Height;
            xPos = args.XPos;
            yPos = args.YPos;
        }

        public override void Setup() => DoSetup(null);
        protected override string CreateOutput(int index, object obj)
            => $"{Path.GetDirectoryName((string)obj)}\\{Path.GetFileNameWithoutExtension((string)obj)}_formatted{Path.GetExtension((string)obj)}";

        protected override string CreateArguments(int index, ref string output, object obj)
        {
            obj = obj as string;

            return $"{(CheckOverwrite(ref output) ? "-y" : string.Empty)} -i \"{obj}\" -vf \"crop={width}:{height}:{xPos}:{yPos}\" \"{output}\"";
        }

        protected override TimeSpan? GetDuration(object obj) => null;
        protected override void CleanUp()
        {
            
        }
    }
}