using System.Windows;
using MVVMFramework.Views;
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
            var types = new[]
            {
                (typeof(SplitterViewModel), "Splitter"),
                (typeof(ConverterViewModel), "Converter"),
                (typeof(FormatterViewModel), "Formatter"),
                (typeof(ReverseViewModel), "Reverse"),
                (typeof(MergerViewModel), "Merger")
            };
            var window = new BaseWindowView(types) { Title = "Video Editor" };
            window.Show();
            base.OnStartup(e);
        }
    }

}
