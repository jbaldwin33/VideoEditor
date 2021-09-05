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

        private void ImagePanel_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) 
                return;

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files == null || !files.Any(f => FormatTypeViewModel.IsVideoFile($".{Path.GetExtension(f)}")))
                MessageBox.Show("cant add non video");//todo
            else
                viewModel.DragFiles?.Invoke(files);
        }
    }
}
