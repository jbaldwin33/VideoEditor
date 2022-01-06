using MVVMFramework.ViewNavigator;
using MVVMFramework.Views;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VideoEditorUi.ViewModels;

namespace VideoEditorUi.Views
{
    /// <summary>
    /// Interaction logic for ResizeView.xaml
    /// </summary>
    public partial class ResizeView : ViewBaseControl
    {
        private ResizerViewModel viewModel;
        public ResizeView() : base(Navigator.Instance.CurrentViewModel)
        {
            InitializeComponent();
            viewModel = Navigator.Instance.CurrentViewModel as ResizerViewModel;
        }

        private void Grid_OnDrop(object sender, DragEventArgs e) => ControlMethods.ImagePanel_Drop(e, viewModel.DragFiles);
        private void OnDragEnter(object sender, DragEventArgs e) => ControlMethods.SetBackgroundBrush(sender as Label, false);

        private void OnPreviewDragLeave(object sender, DragEventArgs e) => ControlMethods.SetBackgroundBrush(sender as Label, true);


        private double Clamp(double val1, double max) => val1 > max ? max : val1;

        private int GCD(int a, int b)
        {
            int remainder;
            while (b != 0)
            {
                remainder = a % b;
                a = b;
                b = remainder;
            }

            return a;
        }
    }
}