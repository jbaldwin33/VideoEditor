using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoUtilities;

namespace VideoEditorUi.Services
{
    public interface IVideoEditorFactory
    {
        BaseClass GetVideoEditor();
        void SetArgs(BaseArgs args);
    }

    public class VideoEditorFactory : IVideoEditorFactory
    {
        private BaseArgs baseArgs;
        private static readonly Lazy<VideoEditorFactory> lazy = new Lazy<VideoEditorFactory>(() => new VideoEditorFactory());
        public static VideoEditorFactory Instance => lazy.Value;

        public void SetArgs(BaseArgs args)
        {
            baseArgs = args;
        }

        public BaseClass GetVideoEditor()
        {
            switch (baseArgs)
            {
                case SplitterArgs splitterArgs: return new VideoSplitter(splitterArgs);
                case ChapterAdderArgs chapterAdderArgs: return new VideoChapterAdder(chapterAdderArgs);
                case SpeedChangerArgs speedChangerArgs: return new VideoSpeedChanger(speedChangerArgs);
                case ConverterArgs converterArgs: return new VideoConverter(converterArgs);
                case ReducerArgs reducerArgs: return new VideoSizeReducer(reducerArgs);
                case ImageCropperArgs imageCropperArgs: return new ImageCropper(imageCropperArgs);
                case ReverserArgs reverserArgs: return new VideoReverser(reverserArgs);
                case MergerArgs mergerArgs: return new VideoMerger(mergerArgs);
                case DownloaderArgs downloaderArgs: return new VideoDownloader(downloaderArgs);
                case VideoCropperArgs videoCropperArgs: return new VideoCropper(videoCropperArgs);
                default: throw new ArgumentOutOfRangeException(nameof(baseArgs), baseArgs, "");
            }
        }
    }
}
