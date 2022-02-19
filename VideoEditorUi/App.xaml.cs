using System;
using System.Windows;
using MVVMFramework.Views;
using MVVMFramework.Localization;
using VideoEditorUi.ViewModels;
using static VideoEditorUi.Utilities.GlobalExceptionHandler;
using System.Threading;
using System.Globalization;
using VideoEditorUi.Utilities;

namespace VideoEditorUi
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            //AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            //Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("ja-JP");
            var types = new[]
            {
                (typeof(SplitterViewModel), new SplitterTranslatable(), true),
                (typeof(ChapterAdderViewModel), new ChapterAdderTranslatable(), true),
                (typeof(SpeedChangerViewModel), new SpeedChangerTranslatable(), true),
                (typeof(ReverseViewModel), new ReverserTranslatable(), true),
                (typeof(MergerViewModel), new MergerTranslatable(), true),
                (typeof(SizeReducerViewModel), $"{new ConverterTranslatable()}/{new ReduceSizeTranslatable()}", true),
                (typeof(ResizerViewModel), new CropperTranslatable(), true),
                //(typeof(ImageCropViewModel), new ImageCropperTranslatable(), true),
                (typeof(DownloaderViewModel), new DownloaderTranslatable(), true)
            };
            var window = new BaseWindowView(types) { Title = new VideoEditorTranslatable() };
            window.Show();
        }
    }
}
