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
        private SizeReducerViewModel viewModel;
        public SizeReducerView() : base(Navigator.Instance.CurrentViewModel)
        {
            InitializeComponent();
            viewModel = Navigator.Instance.CurrentViewModel as SizeReducerViewModel;
        }

        private void ImagePanel_Drop(object sender, DragEventArgs e)
        {

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                viewModel.DragFiles?.Invoke(files);
                // Assuming you have one file that you care about, pass it off to whatever
                // handling code you have defined.
                //HandleFileOpen(files[0]);
            }
        }
    }
}
