using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MVVMFrameworkNet472.ViewNavigator;
using MVVMFrameworkNet472.Views;
using VideoEditorUi.ViewModels;

namespace VideoEditorUi.Views
{
    /// <summary>
    /// Interaction logic for ConverterView.xaml
    /// </summary>
    public partial class ReverseView : ViewBaseControl
    {
        private readonly ReverseViewModel viewModel;
        public ReverseView() : base()
        {
            InitializeComponent();
            playerControl.DataContext = DataContext;
            viewModel = Navigator.Instance.CurrentViewModel as ReverseViewModel;
            playerControl.Initialize();
        }

        private void Grid_OnDrop(object sender, DragEventArgs e) => ControlMethods.ImagePanel_Drop(e, viewModel.DragFiles);
        private void OnDragEnter(object sender, DragEventArgs e) => ControlMethods.SetBackgroundBrush(sender as Label, false);

        private void OnPreviewDragLeave(object sender, DragEventArgs e) => ControlMethods.SetBackgroundBrush(sender as Label, true);
    }
}
