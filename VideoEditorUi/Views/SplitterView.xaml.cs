using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using VideoEditorUi.Singletons;
using VideoEditorUi.ViewModels;
using Path = System.IO.Path;

namespace VideoEditorUi.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class SplitterView : UserControl
    {
        private DispatcherTimer timer;
        private bool isDragging;
        public Action<TimeSpan, TimeSpan> SetRectAndAddAction;
        public Action RectRemoved;
        private SplitterViewModel viewModel;

        public SplitterView()
        {
            InitializeComponent();
            var libsPath = "";
            string directoryName = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            if (directoryName != null)
                libsPath = Path.Combine(directoryName, Path.Combine(Path.Combine("Binaries")));
            player.Init(libsPath, "UserName", "RegKey");
            viewModel = Navigator.Instance.CurrentViewModel as SplitterViewModel;
            viewModel.Player = player;
            viewModel.Slider = slider;
            viewModel.AddRectAndSetEventHandler += ViewModel_AddRectAndSetEventHandler;
            viewModel.ClearAllRectsEventHandler += ViewModel_ClearAllRectsEventHandler;
            timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            timer.Tick += timer_Tick;
            player.MediaOpened += Player_MediaOpened;
            player.MediaEnded += Player_MediaEnded;
            slider.IsMoveToPointEnabled = true;
            slider.ApplyTemplate();
            var thumb = (slider.Template.FindName("PART_Track", slider) as Track).Thumb;
            thumb.MouseEnter += Thumb_MouseEnter;
        }

        private void ViewModel_AddRectAndSetEventHandler(object sender, AddRectEventArgs e) => SetRectAndAdd(e.StartTime, e.EndTime);
        private void ViewModel_ClearAllRectsEventHandler(object sender, EventArgs e) => Dispatcher.Invoke(() => grid.Children.Clear());

        private void SetRectAndAdd(TimeSpan t1, TimeSpan t2)
        {
            grid.Children.Add(new Rectangle());

            var rect = grid.Children[grid.Children.Count - 1] as Rectangle;
            rect.MouseDown += Rect_MouseDown;
            rect.Margin = new Thickness(mapToRange(t1.TotalMilliseconds, 780, slider.Maximum), 0, 0, 0);
            rect.Width = mapToRange((t2 - t1).TotalMilliseconds, 780, slider.Maximum);
            rect.Height = 5;
            rect.HorizontalAlignment = HorizontalAlignment.Left;
            rect.Fill = new SolidColorBrush(Colors.Red);

            double mapToRange(double toConvert, double maxRange1, double maxRange2) => toConvert * (maxRange1 / maxRange2);
        }

        private void Rect_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (MessageBox.Show("Do you want to delete this section?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                var rect = sender as Rectangle;
                var index = grid.Children.IndexOf(rect);
                grid.Children.Remove(rect);
                viewModel.RectRemoved?.Invoke(index);
            }
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (!isDragging)
                slider.Value = player.PositionGet().TotalMilliseconds;
        }

        private void slider_DragStarted(object sender, DragStartedEventArgs e) => isDragging = true;

        private void slider_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            isDragging = false;
            player.PositionSet(TimeSpan.FromMilliseconds(slider.Value));
            viewModel.PositionChanged?.Invoke(player.PositionGet());
        }

        private void Player_MediaOpened(object sender, CSVideoPlayer.MediaOpenedEventArgs e)
        {
            //if (player.NaturalDuration()..HasTimeSpan)
            //{
                var ts = player.NaturalDuration();
                slider.Maximum = ts.TotalMilliseconds;
                slider.SmallChange = 1;
                slider.LargeChange = Math.Min(10, ts.Milliseconds / 10);
            //}
            timer.Start();
        }

        private void Player_MediaEnded(object sender, EventArgs e) => player.Stop();

        private void Thumb_MouseEnter(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                player.PositionSet(new TimeSpan(0, 0, 0, 0, (int)slider.Value));
                viewModel.PositionChanged?.Invoke(player.PositionGet());
            }
        }

        private void player_MediaChanged(object sender, CSVideoPlayer.MediaOpenedEventArgs e)
        {

        }

        private void player_MediaChanging(object sender, CSVideoPlayer.MediaOpeningEventArgs e)
        {

        }

        private void player_MediaClosed(object sender, EventArgs e)
        {

        }

        private void player_MediaOpening(object sender, CSVideoPlayer.MediaOpeningEventArgs e)
        {

        }
    }
}
