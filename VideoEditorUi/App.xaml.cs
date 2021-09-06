using System;
using System.Windows;
using MVVMFramework.Views;
using MVVMFramework.Localization;
using VideoEditorUi.ViewModels;
using static VideoEditorUi.Utilities.GlobalExceptionHandler;

namespace VideoEditorUi
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Dispatcher.UnhandledException += DispatcherOnUnhandledException;
            Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
            //Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("ja-JP");
            var types = new[]
            {
                (typeof(SplitterViewModel), new SplitterTranslatable()),
                (typeof(ChapterAdderViewModel), new ChapterAdderTranslatable()),
                (typeof(SpeedChangerViewModel), new SpeedChangerTranslatable()),
                (typeof(ReverseViewModel), new ReverserTranslatable()),
                (typeof(MergerViewModel), new MergerTranslatable()),
                (typeof(SizeReducerViewModel), $"{new ConverterTranslatable()}/{new ReduceSizeTranslatable()}"),
                (typeof(DownloaderViewModel), new DownloaderTranslatable())
            };
            var window = new BaseWindowView(types) { Title = new VideoEditorTranslatable() };
            window.Show();
            base.OnStartup(e);
        }
    }
}
