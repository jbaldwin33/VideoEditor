using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MVVMFramework.ViewNavigator;
using MVVMFramework.Views;
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
            Utilities.UtilityClass.Instance.InitializePlayer(player);
            viewModel = Navigator.Instance.CurrentViewModel as ReverseViewModel;
            viewModel.Player = player;
        }

        private void Grid_OnDrop(object sender, DragEventArgs e) => ControlMethods.ImagePanel_Drop(e, viewModel.DragFiles);
        private void OnDragEnter(object sender, DragEventArgs e) => ControlMethods.SetBackgroundBrush(sender as Label, false);

        private void OnPreviewDragLeave(object sender, DragEventArgs e) => ControlMethods.SetBackgroundBrush(sender as Label, true);
    }
}
