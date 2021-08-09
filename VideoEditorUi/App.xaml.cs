using System.Windows;
using VideoEditorUi.Singletons;
using VideoEditorUi.ViewModels;
using VideoEditorUi.Views;

namespace VideoEditorUi
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            var window = new MainWindow { DataContext = new MainViewModel(Navigator.Instance) };
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            window.Show();
            base.OnStartup(e);
        }
    }
}
