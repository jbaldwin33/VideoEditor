using System.Globalization;
using System.Threading;
using System.Windows;
using MVVMFramework.Views;
using MVVMFramework;
using MVVMFramework.Localization;
using VideoEditorUi.ViewModels;

namespace VideoEditorUi
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
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
