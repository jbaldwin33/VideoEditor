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
    public partial class SpeedChangerView : ViewBaseControl
    {
        private readonly SpeedChangerViewModel viewModel;
        public SpeedChangerView() : base()
        {
            InitializeComponent();
            playerControl.DataContext = DataContext;
            viewModel = Navigator.Instance.CurrentViewModel as SpeedChangerViewModel;
            viewModel.UpdateSliderEvent = UpdateSlider;
            viewModel.UpdateStackPanelEvent = UpdateStackPanel;
            speedSlider.Value = 1;
            playerControl.Initialize();
            speedSlider.ValueChanged += SpeedSlider_ValueChanged;
        }

        public override void ViewBaseControl_Unloaded(object sender, RoutedEventArgs e)
        {
            speedSlider.ValueChanged -= SpeedSlider_ValueChanged;
            base.ViewBaseControl_Unloaded(sender, e);
        }

        private void UpdateSlider(double value) => speedSlider.Value = value;

        private void UpdateStackPanel(int rotateNumber, int flipScale)
        {
            var transformGroup = new TransformGroup
            {
                Children = new TransformCollection
                {
                    new RotateTransform(rotateNumber * flipScale),
                    new ScaleTransform { ScaleX = flipScale }
                }
            };
            stackPanel.RenderTransformOrigin = new Point(0.5, 0.5);
            stackPanel.LayoutTransform = transformGroup;
        }

        private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => viewModel.CurrentSpeed = e.NewValue;

        private void Grid_OnDrop(object sender, DragEventArgs e) => ControlMethods.ImagePanel_Drop(e, viewModel.DragFiles);
        private void OnDragEnter(object sender, DragEventArgs e) => ControlMethods.SetBackgroundBrush(sender as Label, false);
        private void OnPreviewDragLeave(object sender, DragEventArgs e) => ControlMethods.SetBackgroundBrush(sender as Label, true);
    }
}
