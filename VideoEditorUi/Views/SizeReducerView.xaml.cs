using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MVVMFramework.ViewNavigator;
using MVVMFramework.Views;
using VideoEditorUi.ViewModels;

namespace VideoEditorUi.Views
{
    /// <summary>
    /// Interaction logic for ConverterView.xaml
    /// </summary>
    public partial class SizeReducerView : ViewBaseControl
    {
        private readonly SizeReducerViewModel viewModel;
        public SizeReducerView() : base()
        {
            InitializeComponent();
            viewModel = Navigator.Instance.CurrentViewModel as SizeReducerViewModel;
        }

        private void ImagePanel_Drop(object sender, DragEventArgs e)
        {
            ControlMethods.ImagePanel_Drop(e, viewModel.DragFiles);
            ControlMethods.SetBackgroundBrush(sender as StackPanel, true);
        }
        private void OnDragEnter(object sender, DragEventArgs e) => ControlMethods.SetBackgroundBrush(sender as StackPanel, false);
        private void OnPreviewDragLeave(object sender, DragEventArgs e) => ControlMethods.SetBackgroundBrush(sender as StackPanel, true);
    }
}
