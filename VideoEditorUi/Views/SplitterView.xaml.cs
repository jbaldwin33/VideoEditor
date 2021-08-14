using System;
using System.IO;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using VideoEditorUi.Singletons;
using VideoEditorUi.ViewModels;

namespace VideoEditorUi.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class SplitterView : UserControl
    {
        private DispatcherTimer timer;
        private bool isDragging;
        private SplitterViewModel viewModel;

        public SplitterView()
        {
            InitializeComponent();
            var libsPath = "";
            var directoryName = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            if (directoryName != null)
                libsPath = Path.Combine(directoryName, "Binaries", "CSPlugins", "FFmpeg", IntPtr.Size == 8 ? "x64" : "x86");
            player.Init(libsPath, "UserName", "RegKey");
            viewModel = Navigator.Instance.CurrentViewModel as SplitterViewModel;
            viewModel.Player = player;
            viewModel.Slider = slider;
            timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            timer.Tick += timer_Tick;
            player.MediaOpened += Player_MediaOpened;
            player.MediaEnded += Player_MediaEnded;
            slider.IsMoveToPointEnabled = true;
            slider.ApplyTemplate();
            var thumb = (slider.Template.FindName("PART_Track", slider) as Track).Thumb;
            thumb.MouseEnter += Thumb_MouseEnter;
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
            var ts = player.NaturalDuration();
            slider.Maximum = ts.TotalMilliseconds;
            slider.SmallChange = 1;
            slider.LargeChange = Math.Min(10, ts.Milliseconds / 10);
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
    }
}
