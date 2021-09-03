using System.Windows;
using MVVMFramework.ViewNavigator;
using MVVMFramework.Views;
using VideoEditorUi.ViewModels;

namespace VideoEditorUi.Views
{
    /// <summary>
    /// Interaction logic for ConverterView.xaml
    /// </summary>
    public partial class DownloaderView : ViewBaseControl
    {
        public DownloaderView() : base(Navigator.Instance.CurrentViewModel)
        {
            InitializeComponent();
        }
    }
}
