using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoUtilities
{
    public abstract class BaseArgs
    {
        public string[] InputPaths;

        public BaseArgs(string [] inputPaths)
        {
            InputPaths = inputPaths;
        }
    }
    public class SplitterArgs : BaseArgs
    {
        public List<SectionViewModel> List;
        public bool CombineVideo;
        public bool OutputDifferentFormat;
        public string OutputFormat;
        public bool ReEncodeVideo;

        public SplitterArgs(List<SectionViewModel> list, string inputPath, bool combineVideo, bool outputDifferentFormat, string formatType, bool reEncodeVideo) : base(new string[] { inputPath })
        {
            List = list;
            CombineVideo = combineVideo;
            OutputDifferentFormat = outputDifferentFormat;
            OutputFormat = formatType;
            ReEncodeVideo = reEncodeVideo;
        }
    }

    public class SpeedChangerArgs : BaseArgs
    {
        public double CurrentSpeed;
        public Enums.ScaleRotate ScaleRotate;

        public SpeedChangerArgs(string inputPath, double currentSpeed, Enums.ScaleRotate scaleRotate) : base(new string[] { inputPath })
        {
            CurrentSpeed = currentSpeed;
            ScaleRotate = scaleRotate;
        }
    }

    public class ConverterArgs : BaseArgs
    {
        public List<ConverterPathClass> InputFiles;
        public string OutputFormat;
        public string OutputPath;

        public ConverterArgs(List<ConverterPathClass> inputFiles, string formatType, string outputPath) : base(inputFiles.Select(x => x.FilePath).ToArray())
        {
            InputFiles = inputFiles;
            OutputFormat = formatType;
            OutputPath = outputPath;
        }
    }

    public class ReducerArgs : BaseArgs
    {
        public string OutputPath;

        public ReducerArgs(List<string> inputFiles, string outputPath) : base(inputFiles.ToArray())
        {
            OutputPath = outputPath;
        }
    }

    public class ReverserArgs : BaseArgs
    {
        public ReverserArgs(string inputPath) : base(new string[] { inputPath }) { }
    }

    public class MergerArgs : BaseArgs
    {
        public string OutputPath;
        public string OutputFormat;

        public MergerArgs(List<string> inputFiles, string outputPath, string outputFormat) : base(inputFiles.ToArray())
        {
            OutputPath = outputPath;
            OutputFormat = outputFormat;
        }
    }

    public class ImageCropperArgs : BaseArgs
    {
        public double? Width;
        public double? Height;
        public double? XPos;
        public double? YPos;

        public ImageCropperArgs(string file, double? width, double? height, double? xPos, double? yPos) : base(new string[] { file })
        {
            Width = width;
            Height = height;
            XPos = xPos;
            YPos = yPos;
        }
    }

    public class DownloaderArgs : BaseArgs
    {
        public List<UrlClass> Urls;
        public bool ExtractAudio;
        public string OutputPath;

        public DownloaderArgs(List<UrlClass> urls, bool extractAudio, string outputPath) : base(urls.Select(x => x.Url).ToArray())
        {
            Urls = urls;
            ExtractAudio = extractAudio;
            OutputPath = outputPath;
        }
    }

    public class ChapterAdderArgs : BaseArgs
    {
        public string ImportChapterFile;
        public List<SectionViewModel> Sections;
        public bool DeleteChapterFile;

        public ChapterAdderArgs(string inputPath, string importChapterFile, bool deleteChapterFile) : base(new string[] { inputPath })
        {
            ImportChapterFile = importChapterFile;
            DeleteChapterFile = deleteChapterFile;
        }

        public ChapterAdderArgs(string inputPath, List<SectionViewModel> sections, bool deleteChapterFile) : base(new string[] { inputPath })
        {
            Sections = sections;
            DeleteChapterFile = deleteChapterFile;
        }
    }

    public class VideoCropperArgs : BaseArgs
    {
        public double Width;
        public double Height;
        public double XPos;
        public double YPos;

        public VideoCropperArgs(string inputPath, CropClass cropClass) : base(new string[] { inputPath })
        {
            Width = cropClass.Width;
            Height = cropClass.Height;
            XPos = cropClass.X;
            YPos = cropClass.Y;
        }
    }

    public class CropClass
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
    }

    public class SectionViewModel
    {
        public TimeSpan StartTime;
        public TimeSpan EndTime;
        public string Title;

        public SectionViewModel(TimeSpan start, TimeSpan end, string title)
        {
            StartTime = start;
            EndTime = end;
            Title = title;
        }
    }

    public class ConverterPathClass
    {
        public string FilePath;
        public bool EmbedSubs;

        public ConverterPathClass(string filePath, bool embedSubs)
        {
            FilePath = filePath;
            EmbedSubs = embedSubs;
        }
    }

    public class UrlClass
    {
        public string Url { get; set; }
        public bool IsPlaylist { get; set; }

        public UrlClass(string url, bool isPlaylist)
        {
            Url = isPlaylist || !url.Contains("list") ? url : url.Substring(0, url.IndexOf("list") - 1);
            IsPlaylist = isPlaylist;
        }
    }
}
