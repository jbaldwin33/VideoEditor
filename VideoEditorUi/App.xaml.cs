using System;
using System.Windows;
using MVVMFramework.ViewNavigator;
using MVVMFramework.Views;
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
            var types = new Type[] { typeof(SplitterViewModel), typeof(ConverterViewModel) };
            //var window = new MainWindow(types) { DataContext = new MainViewModel(Navigator.Instance) };
            var window = new BaseWindowView(types)
            {
                DataContext = new MainViewModel(Navigator.Instance),
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            Navigator.Instance.UpdateCurrentViewModelCommand.Execute(types[0]);
            window.Show();
            base.OnStartup(e);
        }
    }
}
