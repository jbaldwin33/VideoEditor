using System.IO;
using System.Linq;
using System.Windows;
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
        public SizeReducerView() : base(Navigator.Instance.CurrentViewModel)
        {
            InitializeComponent();
            viewModel = Navigator.Instance.CurrentViewModel as SizeReducerViewModel;
        }

        private void ImagePanel_Drop(object sender, DragEventArgs e) => ControlMethods.ImagePanel_Drop(e, viewModel.DragFiles);
    }
}
