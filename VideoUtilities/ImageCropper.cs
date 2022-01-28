using System;
using System.IO;

namespace VideoUtilities
{
    public class ImageCropper : BaseClass
    {
        private readonly double? width;
        private readonly double? height;
        private readonly double? xPos;
        private readonly double? yPos;

        public ImageCropper(string fullPath, double? w, double? h, double? x, double? y)
        {
            Failed = false;
            Cancelled = false;
            ShowFile = false;
            OutputPath = $"{Path.GetDirectoryName(fullPath)}\\{Path.GetFileNameWithoutExtension(fullPath)}_formatted{Path.GetExtension(fullPath)}";
            width = w;
            height = h;
            xPos = x;
            yPos = y;
            SetList(new[] { fullPath });
        }

        public override void Setup() => DoSetup(null);
        protected override string CreateOutput(int index, object obj)
            => $"{Path.GetDirectoryName((string)obj)}\\{Path.GetFileNameWithoutExtension((string)obj)}_formatted{Path.GetExtension((string)obj)}";

        protected override string CreateArguments(int index, ref string output, object obj)
        {
            obj = obj as string;

            return $"{(CheckOverwrite(ref output) ? "-y" : string.Empty)} -i \"{obj}\" -frames:v 1 -vf \"pad={(width == null ? $"h={height}" : $"w={width}")}:{(xPos == null ? $"y={yPos}" : $"x={xPos}")}:color=white\" \"{output}\"";
        }

        protected override TimeSpan? GetDuration(object obj) => null;
        protected override void CleanUp()
        {
            
        }
    }
}