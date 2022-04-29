using CSVideoPlayer;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
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
            timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            timer.Tick += TimerTick;
            slider.ApplyTemplate();
            thumb.MouseEnter += ThumbMouseEnter;
            UtilityClass.Instance.InitializePlayer(player);
            viewModel.SliderMax = slider.Maximum;
            viewModel.SeekEvent = Seek;
            viewModel.PlayEvent = () => player.Play();
            viewModel.PauseEvent = () => player.Pause();
            viewModel.GetDetailsEvent = GetPlayerDetails;
            viewModel.OpenEvent = OpenPlayer;
            viewModel.ClosePlayerEvent = ClosePlayer;
            viewModel.GetPlayerPosition = GetPlayerPosition;
            viewModel.SetPlayerPosition = SetPlayerPosition;
        }

        private void TimerTick(object sender, EventArgs e)
        {
            if (!isDragging)
                slider.Value = UtilityClass.Instance.GetPlayerPosition(player).TotalMilliseconds;
        }

        private void PlayerControlUnloaded(object sender, RoutedEventArgs e)
        {
            timer.Tick -= TimerTick;
            thumb.MouseEnter -= ThumbMouseEnter;
        }

        private void SliderDragStarted(object sender, DragStartedEventArgs e) => isDragging = true;

        private void SliderDragCompleted(object sender, DragCompletedEventArgs e)
        {
            isDragging = false;
            UtilityClass.Instance.SetPlayerPosition(player, slider.Value);
            viewModel.PositionChanged?.Invoke(UtilityClass.Instance.GetPlayerPosition(player));
        }

        private void SliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => viewModel.SliderValue = e.NewValue;

        private void UpdateSliderValue(double value)
        {
            slider.Value = value < 0
                ? slider.Value + value < 0 ? 0 : slider.Value + value
                : slider.Value + value > slider.Maximum ? slider.Maximum : slider.Value + value;
        }

        private void Seek(double value)
        {
            UpdateSliderValue(value);
            UtilityClass.Instance.SetPlayerPosition(player, slider.Value);
        }

        private CSMediaProperties.MediaProperties GetPlayerDetails(string file)
        {
            UtilityClass.Instance.GetDetails(player, file);
            return player.mediaProperties;
        }

        private void OpenPlayer(string file) => player.Open(new Uri(file));
        private void ClosePlayer()
        {
            if (player != null)
                UtilityClass.Instance.ClosePlayer(player);
        }
        private TimeSpan GetPlayerPosition() => UtilityClass.Instance.GetPlayerPosition(player);
        private void SetPlayerPosition(double time) => UtilityClass.Instance.SetPlayerPosition(player, time);

        private void PlayerMediaOpened(object sender, MediaOpenedEventArgs e)
        {
            var ts = player.NaturalDuration();
            slider.Maximum = ts.TotalMilliseconds;
            slider.SmallChange = 1;
            slider.LargeChange = Math.Min(10, ts.Milliseconds / 10);
            timer.Start();
        }

        private void PlayerMediaEnded(object sender, EventArgs e) => player.Stop();

        private void ThumbMouseEnter(object sender, MouseEventArgs e)
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
