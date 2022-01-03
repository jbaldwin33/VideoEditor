using MVVMFramework.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using VideoEditorUi.Utilities;
using VideoEditorUi.ViewModels;

namespace VideoEditorUi.Views
{
    /// <summary>
    /// Interaction logic for CropWindow.xaml
    /// </summary>
    public partial class CropWindow : Window
    {
        private enum Anchor { Left, Top, Right, Bottom }
        private bool isDragging = false;
        private double totalWidth;
        private double totalHeight;
        private double clickPosX;
        private double clickPosY;
        private double newWidth;
        private double newHeight;
        private double newX;
        private double newY;
        private ResizerViewModel resizerViewModel;
        private CustomAdorner newAdorner;

        public CropWindow(string filename, ResizerViewModel vm)
        {
            InitializeComponent();
            UtilityClass.GetDetails(player, filename);
            player.Open(new Uri(filename));

            var width = double.Parse(player.mediaProperties.Streams.Stream[0].Width);
            var height = double.Parse(player.mediaProperties.Streams.Stream[0].Height);
            player.Width = width;
            player.Height = height;
            totalWidth = width;
            totalHeight = height;
            gridChild.Width = border.Width = border.MaxWidth = width;
            gridChild.Height = border.Height = border.MaxHeight = height;
            recSelection.Width = width;
            recSelection.Height = height;
            resizerViewModel = vm;
            DataContext = vm;
            resizerViewModel.OriginalWidthHeight = $"width = {width}, height = {height}";
            resizerViewModel.NewWidthHeight = $"new width = {(int)border.Width}, new height = {(int)border.Height}";
            resizerViewModel.Position = $"position = (0,0)";


            Loaded += (sender, e) =>
            {
                var adorner = AdornerLayer.GetAdornerLayer(gridChild);
                newAdorner = new CustomAdorner(border, resizerViewModel);
                adorner.Add(newAdorner);
            };
        }

        #region Rectangle selection

        private void recSelection_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //if (isDragging == false)
            //{
            //    isDragging = true;
            //    clickPosX = Mouse.GetPosition(this).X;
            //    clickPosY = Mouse.GetPosition(this).Y;
            //    Mouse.Capture(sender as Rectangle);
            //}
        }

        private void recSelection_MouseUp(object sender, MouseButtonEventArgs e)
        {
            //if (isDragging)
            //{
            //    isDragging = false;
            //    Mouse.Capture(null);
            //}
        }

        private void recSelection_MouseMove(object sender, MouseEventArgs e)
        {
            /*
            if (isDragging)
            {
                var left = Clamp(e.GetPosition(gridChild).X - clickPosX, 0, totalWidth - recSelection.Width);
                var top = Clamp(e.GetPosition(gridChild).Y - clickPosY, 0, totalHeight - recSelection.Height);
                //border.Margin = new Thickness(left, top, 0, 0);

                var xOffset = e.GetPosition(this).X - clickPosX;
                var yOffset = e.GetPosition(this).Y - clickPosY;
                //var xClamp = Clamp(xOffset, 0, 640);
                //var yClamp = Clamp(yOffset, 0, 360);

                border.RenderTransform = new TranslateTransform { X = xOffset };
                //TODO: make the anchors move like a game
                //var x = Clamp(e.GetPosition(gridChild).X, 0, totalWidth - anchorRight.Margin.Right - 50);
                //UpdateMargin(anchorLeft, x);
                //var y = Clamp(e.GetPosition(gridChild).Y, 0, totalHeight - anchorBottom.Margin.Bottom - 50);
                //UpdateMargin(anchorTop, top: y);
                //var x2 = Clamp(e.GetPosition(gridChild).X, anchorLeft.Margin.Left + 50, totalWidth);
                //UpdateMargin(anchorRight, right: totalWidth - x2);
                //var y2 = Clamp(e.GetPosition(gridChild).Y, anchorTop.Margin.Top + 50, totalHeight);
                //UpdateMargin(anchorBottom, bottom: totalHeight - y2);
                text3.Text = $"position = ({anchorLeft.Margin.Left},{yOffset})";
            }
            */
        }

        #endregion

        private void player_MediaOpened(object sender, CSVideoPlayer.MediaOpenedEventArgs e)
        {
            //var width = double.Parse(player.mediaProperties.Streams.Stream[0].Width);
            //var height = double.Parse(player.mediaProperties.Streams.Stream[0].Height);
            //player.Width = width;
            //player.Height = height;
            //totalWidth = width;
            //totalHeight = height;
            //gridChild.Width = border.MaxWidth = width;
            //gridChild.Height = border.MaxHeight = height;
            //recSelection.Width = width;
            //recSelection.Height = height;

            //text1.Text = $"width = {width}, height = {height}";
            //text2.Text = $"new width = {(int)border.Width}, new height = {(int)border.Height}";
            //text3.Text = $"position = (0,0)";
        }

        private double Clamp(double val, double min, double max) => val > max ? max : val < min ? min : val;


        #region Border

        //private void border_MouseMove(object sender, MouseEventArgs e)
        //{
        //    if (e.OriginalSource is Rectangle)
        //        return;

        //    if (isDragging)
        //    {
        //        var x = e.GetPosition(gridChild).X;
        //        var y = e.GetPosition(gridChild).Y;
        //        var posLeft = border.Margin.Left;
        //        var posTop = border.Margin.Top;
        //        //border.Margin = new Thickness(0, 0, 0, 0);
        //        border.Width = Clamp(x - posLeft, 50, totalWidth);
        //        border.Height = Clamp(y - posTop, 50, totalHeight);
        //        recSelection.Width = Clamp(x - posLeft, 50, totalWidth);
        //        recSelection.Height = Clamp(y - posTop, 50, totalHeight);
        //        text2.Text = $"new width = {(int)border.Width}, new height = {(int)border.Height}";
        //    }
        //}

        //private void border_MouseDown(object sender, MouseButtonEventArgs e)
        //{
        //    if (e.OriginalSource is Rectangle)
        //        return;

        //    if (isDragging == false)
        //    {
        //        isDragging = true;
        //        Mouse.Capture(sender as Border);
        //    }
        //}

        //private void border_MouseUp(object sender, MouseButtonEventArgs e)
        //{
        //    if (isDragging)
        //    {
        //        isDragging = false;
        //        Mouse.Capture(null);
        //    }
        //}

        #endregion

        #region Anchor

        private void anchor_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (isDragging == false)
            {
                isDragging = true;
                Mouse.Capture(sender as Rectangle);
            }
        }

        private void anchor_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
                Mouse.Capture(null);
            }
        }

        private void MoveAnchor(Anchor anchor, Point position)
        {
            //if (isDragging)
            //{
            //    switch (anchor)
            //    {
            //        case Anchor.Left:
            //            var x = Clamp(position.X, 0, totalWidth - anchorRight.Margin.Right - 50);
            //            UpdateMargin(border, x);
            //            UpdateMargin(anchorLeft, x);
            //            UpdateMargin(anchorTop, x);
            //            UpdateMargin(anchorBottom, x);
            //            break;
            //        case Anchor.Top:
            //            var y = Clamp(position.Y, 0, totalHeight - anchorBottom.Margin.Bottom - 50);
            //            UpdateMargin(border, top: y);
            //            UpdateMargin(anchorLeft, top: y);
            //            UpdateMargin(anchorTop, top: y);
            //            UpdateMargin(anchorRight, top: y);
            //            break;
            //        case Anchor.Right:
            //            x = Clamp(position.X, anchorLeft.Margin.Left + 50, totalWidth);
            //            UpdateMargin(border, right: totalWidth - x);
            //            UpdateMargin(anchorTop, right: totalWidth - x);
            //            UpdateMargin(anchorRight, right: totalWidth - x);
            //            UpdateMargin(anchorBottom, right: totalWidth - x);
            //            break;
            //        case Anchor.Bottom:
            //            y = Clamp(position.Y, anchorTop.Margin.Top + 50, totalHeight);
            //            UpdateMargin(border, bottom: totalHeight - y);
            //            UpdateMargin(anchorLeft, bottom: totalHeight - y);
            //            UpdateMargin(anchorRight, bottom: totalHeight - y);
            //            UpdateMargin(anchorBottom, bottom: totalHeight - y);
            //            break;
            //        default: break;
            //    }
            //    newX = Clamp(position.X - clickPosX, 0, anchorLeft.Margin.Left);
            //    newY = Clamp(position.Y - clickPosY, 0, anchorTop.Margin.Top);
            //    newWidth = (int)recSelection.Width - anchorLeft.Margin.Left - anchorRight.Margin.Right;
            //    newHeight = (int)recSelection.Height - anchorTop.Margin.Top - anchorBottom.Margin.Bottom;
            //    text2.Text = $"new width = {newWidth}, new height = {newHeight}";
            //    text3.Text = $"position = ({newX},{newY})";
            //}
        }

        private void anchor_MouseMove(object sender, MouseEventArgs e)
        {
            Anchor a;
            var rect = sender as Rectangle;
            switch (rect.Name)
            {
                case "anchorLeft": a = Anchor.Left; break;
                case "anchorTop": a = Anchor.Top; break;
                case "anchorRight": a = Anchor.Right; break;
                case "anchorBottom": a = Anchor.Bottom; break;
                default: throw new ArgumentOutOfRangeException(rect.Name);
            }
            MoveAnchor(a, e.GetPosition(gridChild));
        }

        #endregion

        private void UpdateMargin(FrameworkElement element, double? left = null, double? top = null, double? right = null, double? bottom = null) =>
            element.Margin = new Thickness(
                left == null ? element.Margin.Left : left.Value,
                top == null ? element.Margin.Top : top.Value,
                right == null ? element.Margin.Right : right.Value,
                bottom == null ? element.Margin.Bottom : bottom.Value);

        private void MediumButton_Click(object sender, RoutedEventArgs e)
        {
            resizerViewModel.CropClass = new CropClass { Width = newAdorner.childElement.Width, Height = newAdorner.childElement.Height, X = newAdorner.LeftPos, Y = newAdorner.TopPos };
            Close();
        }
    }
}
