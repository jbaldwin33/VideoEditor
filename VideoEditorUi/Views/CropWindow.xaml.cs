using CSVideoPlayer;
using System;
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
        public ResizeAdorner Adorner;
        private readonly ResizerViewModel resizerViewModel;
        private bool isDragging = false;
        private double totalWidth;
        private double totalHeight;
        //private double origCanvasLeft;
        //private double origCanvasTop;
        //private Point clickPosition;
        //private TranslateTransform originTT;
        private Thumb _thumb;
        private Thumb thumb => _thumb ?? (_thumb = (slider.Template.FindName("PART_Track", slider) as Track)?.Thumb);
        private DispatcherTimer timer;

        public CropWindow(string filename, ResizerViewModel vm)
        {
            InitializeComponent();
            UtilityClass.Instance.GetDetails(player, filename);
            player.Open(new Uri(filename));

            var width = double.Parse(player.mediaProperties.Streams.Stream[0].Width);
            var height = double.Parse(player.mediaProperties.Streams.Stream[0].Height);
            player.Width = gridChild.Width = border.Width = border.MaxWidth = totalWidth = recSelection.Width = width;
            player.Height = gridChild.Height = border.Height = border.MaxHeight = totalHeight = recSelection.Height = height;
            resizerViewModel = vm;
            DataContext = vm;
            //resizerViewModel.Player = player;
            //resizerViewModel.Slider = slider;
            resizerViewModel.OldSize = $"Old size: {width}x{height}";
            resizerViewModel.NewSize = $"New size: {border.Width}x{border.Height}";

            timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            timer.Tick += timer_Tick;
            player.MediaOpened += Player_MediaOpened;
            player.MediaEnded += Player_MediaEnded;
            slider.ApplyTemplate();
            thumb.MouseEnter += Thumb_MouseEnter;
            Unloaded += CropWindow_Unloaded;

            Loaded += (sender, e) =>
            {
                var adornerLayer = AdornerLayer.GetAdornerLayer(gridChild);
                Adorner = new ResizeAdorner(border, resizerViewModel)
                {
                    ChildWidth = border.Width,
                    ChildHeight = border.Height
                };
                //origCanvasLeft = Canvas.GetLeft(border);
                //origCanvasTop = Canvas.GetTop(border);
                adornerLayer.Add(Adorner);
            };
        }

        private void CropWindow_Unloaded(object sender, RoutedEventArgs e)
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
            resizerViewModel.PositionChanged?.Invoke(UtilityClass.Instance.GetPlayerPosition(player));
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
                resizerViewModel.PositionChanged?.Invoke(UtilityClass.Instance.GetPlayerPosition(player));
            }
        }

        private void recSelection_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //var draggableControl = sender as Canvas;
            //originTT = draggableControl.RenderTransform as TranslateTransform ?? new TranslateTransform();
            //isDragging = true;
            //clickPosition = e.GetPosition(this);
            //draggableControl.CaptureMouse();
        }

        private void recSelection_MouseUp(object sender, MouseButtonEventArgs e)
        {
            //isDragging = false;
            //var draggable = sender as Canvas;
            //draggable.ReleaseMouseCapture();
        }

        private void recSelection_MouseMove(object sender, MouseEventArgs e)
        {
            //if (isDragging && sender is Canvas draggableControl)
            //{
            //    var currentPosition = e.GetPosition(this);
            //    var transform = draggableControl.RenderTransform as TranslateTransform ?? new TranslateTransform();
            //    var deltaX = currentPosition.X - clickPosition.X;
            //    var deltaY = currentPosition.Y - clickPosition.Y;
            //    if (Canvas.GetLeft(border) != origCanvasLeft && (deltaX < 0 || deltaX > 0))
            //        transform.X = Clamp(originTT.X + deltaX, origCanvasLeft - Canvas.GetLeft(border), totalWidth - Adorner.ChildWidth - (totalWidth - Adorner.ChildWidth));
            //    else if (Canvas.GetLeft(border) == origCanvasLeft && (deltaX < 0 || deltaX > 0))
            //        transform.X = Clamp(originTT.X + deltaX, origCanvasLeft - Canvas.GetLeft(border), totalWidth - Adorner.ChildWidth);

            //    if (Canvas.GetTop(border) != origCanvasTop && (deltaY < 0 || deltaY > 0))
            //        transform.Y = Clamp(originTT.Y + deltaY, origCanvasTop - Canvas.GetTop(border), totalHeight - Adorner.ChildHeight - (totalHeight - Adorner.ChildHeight));
            //    else if (Canvas.GetTop(border) == origCanvasTop && (deltaY < 0 || deltaY > 0))
            //        transform.Y = Clamp(originTT.Y + deltaY, origCanvasTop - Canvas.GetTop(border), totalHeight - Adorner.ChildHeight);
            //    draggableControl.RenderTransform = new TranslateTransform(transform.X, transform.Y);

            //    //Adorner.LeftPos = transform.X;
            //    //Adorner.TopPos = transform.Y;
            //    //Adorner.UpdateText();
            //}
        }

        private double Clamp(double val, double min, double max) => val > max ? max : val < min ? min : val;

        private void MediumButton_Click(object sender, RoutedEventArgs e)
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
    }
}
