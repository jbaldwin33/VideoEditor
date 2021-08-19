using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using CSVideoPlayer;
using MVVMFramework.ViewNavigator;
using MVVMFramework.Views;
using VideoEditorUi.ViewModels;
using static VideoEditorUi.Utilities.UtilityClass;

namespace VideoEditorUi.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class SplitterView : ViewBaseControl
    {
        private DispatcherTimer timer;
        private bool isDragging;
        private SplitterViewModel viewModel;
        //private VideoPlayerWPF player;

        public SplitterView() : base(Navigator.Instance.CurrentViewModel)
        {
            InitializeComponent();
            viewModel = Navigator.Instance.CurrentViewModel as SplitterViewModel;
            //viewModel.CreateNewPlayer = CreatePlayer;
            viewModel.Slider = slider;
            timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            timer.Tick += timer_Tick;
            InitializePlayer(player);
            viewModel.Player = player;
            player.MediaOpened += Player_MediaOpened;
            player.MediaEnded += Player_MediaEnded;
            slider.ApplyTemplate();
            var thumb = (slider.Template.FindName("PART_Track", slider) as Track).Thumb;
            thumb.MouseEnter += Thumb_MouseEnter;
        }
        
        //private void CreatePlayer()
        //{
            
        //    //player.Dispose();
        //    playerPanel.Children.Clear();
        //    player = new VideoPlayerWPF
        //    {
        //        Name = "player",
        //        Height = 250,
        //        VerticalAlignment = VerticalAlignment.Top,
        //        VerticalContentAlignment = VerticalAlignment.Center,
        //        HorizontalAlignment = HorizontalAlignment.Center
        //    };
        //    playerPanel.Children.Add(player);
        //    InitializePlayer(player);
        //    viewModel.Player = player;
        //    player.MediaOpened += Player_MediaOpened;
        //    player.MediaEnded += Player_MediaEnded;
        //}

        private void timer_Tick(object sender, EventArgs e)
        {
            if (!isDragging)
                slider.Value = GetPlayerPosition(player).TotalMilliseconds;
        }

        private void slider_DragStarted(object sender, DragStartedEventArgs e) => isDragging = true;

        private void slider_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            isDragging = false;
            SetPlayerPosition(player, slider.Value);
            viewModel.PositionChanged?.Invoke(GetPlayerPosition(player));
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
                SetPlayerPosition(player, slider.Value);
                viewModel.PositionChanged?.Invoke(GetPlayerPosition(player));
            }
        }
    }
}
