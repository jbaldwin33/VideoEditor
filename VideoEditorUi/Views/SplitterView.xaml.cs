using MVVMFrameworkNet472.Views;
using System;
using System.Windows;
using System.Windows.Media;
using VideoEditorUi.ViewModels;

namespace VideoEditorUi.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class SplitterView : ViewBaseControl
    {
        private readonly SplitterViewModel vm;
        public SplitterView() : base()
        {
            InitializeComponent();
            playerControl.DataContext = DataContext;
            vm = DataContext as SplitterViewModel;
            vm.AddRectangleEvent = AddRectangle;
            playerControl.Initialize();
        }

        private void AddRectangle()
        {
            var rect = new RectClass
            {
                RectCommand = vm.RectCommand,
                Margin = new Thickness(mapToRange(vm.StartTime.TotalMilliseconds, 760, playerControl.slider.Maximum), 0, 0, 0),
                Width = mapToRange((vm.EndTime - vm.StartTime).TotalMilliseconds, 760, playerControl.slider.Maximum),
                Height = 5,
                HorizontalAlignment = HorizontalAlignment.Left,
                Fill = new SolidColorBrush(Colors.Red)
            };
            vm.RectCollection.Add(rect);
            double mapToRange(double toConvert, double maxRange1, double maxRange2) => toConvert * (maxRange1 / maxRange2);
        }
    }
}
