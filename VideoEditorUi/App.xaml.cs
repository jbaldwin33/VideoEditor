using System;
using System.Windows;
using MVVMFramework.Views;
using MVVMFramework.Localization;
using VideoEditorUi.ViewModels;
using static VideoEditorUi.Utilities.GlobalExceptionHandler;
using System.Threading;
using System.Globalization;
using VideoEditorUi.Utilities;
using VideoEditorUi.Services;
using MVVMFramework.ViewModels;

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
            var utilityClass = UtilityClass.Instance;
            var editorService = VideoEditorService.Instance;
            var types = new (ViewModel, bool)[]
            {
                (new SplitterViewModel(utilityClass, editorService), true),
                (new ChapterAdderViewModel(utilityClass, editorService), true),
                (new SpeedChangerViewModel(utilityClass, editorService), true),
                (new ReverseViewModel(utilityClass, editorService), true),
                (new MergerViewModel(utilityClass, editorService), true),
                (new SizeReducerViewModel(utilityClass, editorService), true),
                (new ResizerViewModel(utilityClass, editorService), true),
                //(new ImageCropViewModel(utilityClass, editorService), true),
                (new DownloaderViewModel(utilityClass, editorService), true)
            };
            var window = new BaseWindowView(types) { Title = new VideoEditorTranslatable() };
            window.Show();
        }
    }
}
