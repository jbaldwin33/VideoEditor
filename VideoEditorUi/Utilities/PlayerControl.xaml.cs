using CSVideoPlayer;
using MVVMFramework.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using VideoEditorUi.ViewModels;
using VideoEditorUi.Views;

namespace VideoEditorUi.Utilities
{
    /// <summary>
    /// Interaction logic for PlayerControl.xaml
    /// </summary>
    public partial class PlayerControl : UserControl
    {
        private Thumb _thumb;
        private Thumb thumb => _thumb ?? (_thumb = (slider.Template.FindName("PART_Track", slider) as Track)?.Thumb);
        private DispatcherTimer timer;
        private bool isDragging;
        private EditorViewModel viewModel;

        public PlayerControl()
        {
            InitializeComponent();
        }

        public void Initialize()
        {
            viewModel = DataContext as EditorViewModel;
            viewModel.Slider = slider;
            timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            timer.Tick += timer_Tick;
            UtilityClass.Instance.InitializePlayer(player);
            viewModel.Player = player;
            player.MediaOpened += Player_MediaOpened;
            player.MediaEnded += Player_MediaEnded;
            slider.ApplyTemplate();
            thumb.MouseEnter += Thumb_MouseEnter;
            Unloaded += PlayerControl_Unloaded;
        }

        private void PlayerControl_Unloaded(object sender, RoutedEventArgs e)
        {
            player.MediaOpened -= Player_MediaOpened;
            player.MediaEnded -= Player_MediaEnded;
            timer.Tick -= timer_Tick;
            thumb.MouseEnter -= Thumb_MouseEnter;
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (!isDragging)
                slider.Value = UtilityClass.Instance.GetPlayerPosition(player).TotalMilliseconds;
        }

        private void slider_DragStarted(object sender, DragStartedEventArgs e) => isDragging = true;

        private void slider_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            isDragging = false;
            UtilityClass.Instance.SetPlayerPosition(player, slider.Value);
            viewModel.PositionChanged?.Invoke(UtilityClass.Instance.GetPlayerPosition(player));
        }

        private void Player_MediaOpened(object sender, MediaOpenedEventArgs e)
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
                UtilityClass.Instance.SetPlayerPosition(player, slider.Value);
                viewModel.PositionChanged?.Invoke(UtilityClass.Instance.GetPlayerPosition(player));
            }
        }

        private void Grid_OnDrop(object sender, DragEventArgs e) => ControlMethods.ImagePanel_Drop(e, viewModel.DragFiles);

        private void OnDragEnter(object sender, DragEventArgs e) => ControlMethods.SetBackgroundBrush(sender as Label, false);

        private void OnPreviewDragLeave(object sender, DragEventArgs e) => ControlMethods.SetBackgroundBrush(sender as Label, true);
    }
}
