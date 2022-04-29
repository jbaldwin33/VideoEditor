using CSVideoPlayer;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using VideoEditorUi.Utilities;
using VideoEditorUi.ViewModels;
using VideoUtilities;

namespace VideoEditorUi.Views
{
    /// <summary>
    /// Interaction logic for CropWindow.xaml
    /// </summary>
    public partial class CropWindow : Window
    {
        private readonly ResizerViewModel resizerViewModel;
        private readonly DispatcherTimer timer;
        private bool isDragging = false;
        private Thumb _thumb;
        private Thumb thumb => _thumb ?? (_thumb = (slider.Template.FindName("PART_Track", slider) as Track)?.Thumb);
        public ResizeAdorner Adorner;

        public CropWindow(string filename, ResizerViewModel vm)
        {
            InitializeComponent();
            UtilityClass.Instance.GetDetails(player, filename);
            player.Open(new Uri(filename));
            var videoStream = player.mediaProperties.Streams.Stream.First(x => x.CodecType == "video");
            var width = double.Parse(videoStream.Width);
            var height = double.Parse(videoStream.Height);
            player.Width = gridChild.Width = border.Width = border.MaxWidth = recSelection.Width = width;
            player.Height = gridChild.Height = border.Height = border.MaxHeight = recSelection.Height = height;
            resizerViewModel = vm;

            timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            timer.Tick += TimerTick;
            slider.ApplyTemplate();
            thumb.MouseEnter += ThumbMouseEnter;
            SetViewModelEvents();
            var cropVM = CreateViewModel();


            DataContext = cropVM;
            Loaded += (sender, e) =>
            {
                var adornerLayer = AdornerLayer.GetAdornerLayer(gridChild);
                Adorner = new ResizeAdorner(border, cropVM);
                adornerLayer.Add(Adorner);
            };
        }

        private CropWindowViewModel CreateViewModel()
        {
            var vm = new CropWindowViewModel
            {
                CropClass = resizerViewModel.CropClass,
                SetCrop = MediumButton_Click,
            };

            if (resizerViewModel.CropClass == null)
            {
                vm.OldSize = $"Old size: {border.Width}x{border.Height}";
                vm.NewSize = $"New size: {border.Width}x{border.Height}";
                vm.Position = $"Position: (0,0)";
            }
            else
            {
                vm.OldSize = resizerViewModel.OldSizeString;
                vm.NewSize = resizerViewModel.NewSizeString;
                vm.Position = resizerViewModel.PositionString;
            }
            return vm;
        }

        public void Initialize()
        {
            resizerViewModel.OldSizeString = $"Old size: {border.Width}x{border.Height}";
            resizerViewModel.NewSizeString = $"New size: {border.Width}x{border.Height}";
        }

        private void SetViewModelEvents()
        {
            resizerViewModel.SliderMax = slider.Maximum;
            resizerViewModel.SeekEvent = Seek;
            resizerViewModel.PlayEvent = () => player.Play();
            resizerViewModel.PauseEvent = () => player.Pause();
            resizerViewModel.GetDetailsEvent = GetPlayerDetails;
            resizerViewModel.OpenEvent = OpenPlayer;
            resizerViewModel.ClosePlayerEvent = ClosePlayer;
            resizerViewModel.GetPlayerPosition = GetPlayerPosition;
            resizerViewModel.SetPlayerPosition = SetPlayerPosition;
        }

        private void CropWindowUnloaded(object sender, RoutedEventArgs e)
        {
            timer.Tick -= TimerTick;
            thumb.MouseEnter -= ThumbMouseEnter;
        }

        private void MediumButton_Click()
        {
            resizerViewModel.CropClass = new CropClass
            {
                Width = Adorner.ChildWidth,
                Height = Adorner.ChildHeight,
                X = Adorner.LeftPos,
                Y = Adorner.TopPos
            };
            Close();
        }

        private void TimerTick(object sender, EventArgs e)
        {
            if (!isDragging)
                slider.Value = UtilityClass.Instance.GetPlayerPosition(player).TotalMilliseconds;
        }

        private void ThumbMouseEnter(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                UtilityClass.Instance.SetPlayerPosition(player, slider.Value);
                resizerViewModel.PositionChanged?.Invoke(UtilityClass.Instance.GetPlayerPosition(player));
            }
        }

        #region Slider methods

        private void SliderDragStarted(object sender, DragStartedEventArgs e) => isDragging = true;

        private void SliderDragCompleted(object sender, DragCompletedEventArgs e)
        {
            isDragging = false;
            UtilityClass.Instance.SetPlayerPosition(player, slider.Value);
            resizerViewModel.PositionChanged?.Invoke(UtilityClass.Instance.GetPlayerPosition(player));
        }

        private void SliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => resizerViewModel.SliderValue = e.NewValue;

        private void UpdateSliderValue(double value)
        {
            slider.Value = value < 0
                ? slider.Value + value < 0 ? 0 : slider.Value + value
                : slider.Value + value > slider.Maximum ? slider.Maximum : slider.Value + value;
        }

        #endregion

        #region Player methods

        private void PlayerMediaOpened(object sender, MediaOpenedEventArgs e)
        {
            var ts = player.NaturalDuration();
            slider.Maximum = ts.TotalMilliseconds;
            slider.SmallChange = 1;
            slider.LargeChange = Math.Min(10, ts.Milliseconds / 10);
            timer.Start();
        }

        private void PlayerMediaEnded(object sender, EventArgs e) => player.Stop();
        private void Seek(double value)
        {
            UpdateSliderValue(value);
            UtilityClass.Instance.SetPlayerPosition(player, slider.Value);
        }

        private void ClosePlayer()
        {
            if (player != null)
                UtilityClass.Instance.ClosePlayer(player);
        }

        private CSMediaProperties.MediaProperties GetPlayerDetails(string file)
        {
            UtilityClass.Instance.GetDetails(player, file);
            return player.mediaProperties;
        }

        private void OpenPlayer(string file) => player.Open(new Uri(file));
        private TimeSpan GetPlayerPosition() => UtilityClass.Instance.GetPlayerPosition(player);
        private void SetPlayerPosition(double time) => UtilityClass.Instance.SetPlayerPosition(player, time);

        #endregion
    }
}
