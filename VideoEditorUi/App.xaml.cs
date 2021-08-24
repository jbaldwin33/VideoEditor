using System.Globalization;
using System.Threading;
using System.Windows;
using MVVMFramework.Views;
using MVVMFramework;
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
                (typeof(SplitterViewModel), Translatables.Splitter),
                (typeof(ConverterViewModel), Translatables.Converter),
                (typeof(SpeedChangerViewModel), Translatables.SpeedChanger),
                (typeof(ReverseViewModel), Translatables.Reverser),
                (typeof(MergerViewModel), Translatables.Merger),
            };
            var window = new BaseWindowView(types) { Title = Translatables.VideoEditor };
            window.Show();
            base.OnStartup(e);
        }
    }

}
