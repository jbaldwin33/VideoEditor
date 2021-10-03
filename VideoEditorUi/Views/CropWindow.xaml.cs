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

        public CropWindow(string filename)
        {
            InitializeComponent();
            UtilityClass.GetDetails(player, filename);
            player.Open(new Uri(filename));
        }

        private void rectangle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (isDragging == false)
            {
                isDragging = true;
                clickPosX = e.GetPosition(recSelection).X;
                clickPosY = e.GetPosition(recSelection).Y;
                Mouse.Capture(sender as Rectangle);
            }
        }

        private void rectangle_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
                Mouse.Capture(null);
            }
        }

        private void rectangle_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                var left = Clamp(e.GetPosition(gridChild).X - clickPosX, 0, totalWidth - recSelection.Width);
                var top = Clamp(e.GetPosition(gridChild).Y - clickPosY, 0, totalHeight - recSelection.Height);
                border.Margin = new Thickness(left, top, 0, 0);
                
                text3.Text = $"position = ({left},{top})";
            }
        }

        private void player_MediaOpened(object sender, CSVideoPlayer.MediaOpenedEventArgs e)
        {
            var width = double.Parse(player.mediaProperties.Streams.Stream[0].Width);
            var height = double.Parse(player.mediaProperties.Streams.Stream[0].Height);
            player.Width = width;
            player.Height = height;
            totalWidth = width;
            totalHeight = height;
            gridChild.Width = recSelection.Width = width;
            gridChild.Height = recSelection.Height = height;

            text1.Text = $"width = {width}, height = {height}";
            text2.Text = $"new width = {(int)border.Width}, new height = {(int)border.Height}";
            text3.Text = $"position = (0,0)";
        }

        private double Clamp(double val, double min, double max) => val > max ? max : val < min ? min : val;
        //private double ClampMin(double val, double min) => val < min ? min : val;

        private void border_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.OriginalSource is Rectangle)
                return;

            if (isDragging)
            {
                var x = e.GetPosition(gridChild).X;
                var y = e.GetPosition(gridChild).Y;
                var posLeft = border.Margin.Left;
                var posTop = border.Margin.Top;
                //border.Margin = new Thickness(0, 0, 0, 0);
                border.Width = Clamp(x - posLeft, 50, totalWidth);
                border.Height = Clamp(y - posTop, 50, totalHeight);
                recSelection.Width = Clamp(x - posLeft, 50, totalWidth);
                recSelection.Height = Clamp(y - posTop, 50, totalHeight);
                text2.Text = $"new width = {(int)border.Width}, new height = {(int)border.Height}";
            }
        }

        private void border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is Rectangle)
                return;

            if (isDragging == false)
            {
                isDragging = true;
                Mouse.Capture(sender as Border);
            }
        }

        private void border_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
                Mouse.Capture(null);
            }
        }

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
        private void UpdateMargin(FrameworkElement element, double? left = null, double? top = null, double? right = null, double? bottom = null)
        {
            element.Margin = new Thickness(
                left == null ? element.Margin.Left : left.Value,
                top == null ? element.Margin.Top : top.Value,
                right == null ? element.Margin.Right : right.Value,
                bottom == null ? element.Margin.Bottom : bottom.Value);
        }
        private void MoveAnchor(Anchor anchor, Point position)
        {
            if (isDragging)
            {
                switch (anchor)
                {
                    case Anchor.Left:
                        var x = Clamp(position.X, 0, totalWidth - anchorRight.Margin.Right - 50);
                        UpdateMargin(border, x);
                        UpdateMargin(anchorLeft, x);
                        UpdateMargin(anchorTop, x);
                        UpdateMargin(anchorBottom, x);
                        //border.Margin = new Thickness(x, border.Margin.Top, border.Margin.Right, border.Margin.Bottom);
                        //anchorLeft.Margin = new Thickness(x, anchorLeft.Margin.Top, anchorLeft.Margin.Right, anchorLeft.Margin.Bottom);
                        //anchorTop.Margin = new Thickness(x, anchorTop.Margin.Top, anchorTop.Margin.Right, anchorTop.Margin.Bottom);
                        //anchorBottom.Margin = new Thickness(x, anchorBottom.Margin.Top, anchorBottom.Margin.Right, anchorBottom.Margin.Bottom);
                        break;
                    case Anchor.Top:
                        var y = Clamp(position.Y, 0, totalHeight - anchorBottom.Margin.Bottom - 50);
                        UpdateMargin(border, top: y);
                        UpdateMargin(anchorLeft, top: y);
                        UpdateMargin(anchorTop, top: y);
                        UpdateMargin(anchorRight, top: y);
                        //border.Margin = new Thickness(border.Margin.Left, y, border.Margin.Right, border.Margin.Bottom);
                        //anchorLeft.Margin = new Thickness(anchorLeft.Margin.Left, y, anchorLeft.Margin.Right, anchorLeft.Margin.Bottom);
                        //anchorTop.Margin = new Thickness(anchorTop.Margin.Left, y, anchorTop.Margin.Right, anchorTop.Margin.Bottom);
                        //anchorRight.Margin = new Thickness(anchorRight.Margin.Left, y, anchorRight.Margin.Right, anchorRight.Margin.Bottom);
                        break;
                    case Anchor.Right:
                        x = Clamp(position.X, anchorLeft.Margin.Left + 50, totalWidth);
                        UpdateMargin(border, right: totalWidth - x);
                        UpdateMargin(anchorTop, right: totalWidth - x);
                        UpdateMargin(anchorRight, right: totalWidth - x);
                        UpdateMargin(anchorBottom, right: totalWidth - x);
                        //border.Margin = new Thickness(border.Margin.Left, border.Margin.Top, totalWidth - x, border.Margin.Bottom);
                        //anchorTop.Margin = new Thickness(anchorTop.Margin.Left, anchorTop.Margin.Top, totalWidth - x, anchorTop.Margin.Bottom);
                        //anchorRight.Margin = new Thickness(anchorRight.Margin.Left, anchorRight.Margin.Top, totalWidth - x, anchorRight.Margin.Bottom);
                        //anchorBottom.Margin = new Thickness(anchorBottom.Margin.Left, anchorBottom.Margin.Top, totalWidth - x, anchorBottom.Margin.Bottom);
                        break;
                    case Anchor.Bottom:
                        y = Clamp(position.Y, anchorTop.Margin.Top + 50, totalHeight);
                        UpdateMargin(border, bottom: totalHeight - y);
                        UpdateMargin(anchorLeft, bottom: totalHeight - y);
                        UpdateMargin(anchorRight, bottom: totalHeight - y);
                        UpdateMargin(anchorBottom, bottom: totalHeight - y);
                        //border.Margin = new Thickness(border.Margin.Left, border.Margin.Top, border.Margin.Right, totalHeight - y);
                        //anchorLeft.Margin = new Thickness(anchorLeft.Margin.Left, anchorLeft.Margin.Top, anchorLeft.Margin.Right, totalHeight - y);
                        //anchorRight.Margin = new Thickness(anchorRight.Margin.Left, anchorRight.Margin.Top, anchorRight.Margin.Right, totalHeight - y);
                        //anchorBottom.Margin = new Thickness(anchorBottom.Margin.Left, anchorBottom.Margin.Top, anchorBottom.Margin.Right, totalHeight - y);
                        break;
                    default: break;
                }
                var left = Clamp(position.X - clickPosX, 0, anchorLeft.Margin.Left);
                var top = Clamp(position.Y - clickPosY, 0, anchorTop.Margin.Top);
                var newWidth = (int)recSelection.Width - anchorLeft.Margin.Left - anchorRight.Margin.Right;
                var newHeight = (int)recSelection.Height - anchorTop.Margin.Top - anchorBottom.Margin.Bottom;
                text2.Text = $"new width = {newWidth}, new height = {newHeight}";
                text3.Text = $"position = ({left},{top})";
            }
        }

        //left anchor
        private void anchor_MouseMove(object sender, MouseEventArgs e)
        {
            Anchor a = Anchor.Left;
            Rectangle rect = sender as Rectangle;
            switch (rect.Name)
            {
                case "anchorLeft": a = Anchor.Left; break;
                case "anchorTop": a = Anchor.Top; break;
                case "anchorRight": a = Anchor.Right; break;
                case "anchorBottom": a = Anchor.Bottom; break;
            }
            MoveAnchor(a, e.GetPosition(gridChild));
            //if (isDragging)
            //{
            //    var x = Clamp(e.GetPosition(gridChild).X, 0, totalWidth - 50);
            //    var y = e.GetPosition(gridChild).Y;
            //    var posLeft = border.Margin.Left;
            //    var posTop = border.Margin.Top;
            //    border.Margin = new Thickness(x, border.Margin.Top, border.Margin.Right, border.Margin.Bottom);
            //    anchorLeft.Margin = new Thickness(x, anchorLeft.Margin.Top, anchorLeft.Margin.Right, anchorLeft.Margin.Bottom);
            //    anchorTop.Margin = new Thickness(x, anchorTop.Margin.Top, anchorTop.Margin.Right, anchorTop.Margin.Bottom);
            //    anchorRight.Margin = new Thickness(x, anchorRight.Margin.Top, anchorRight.Margin.Right, anchorRight.Margin.Bottom);
            //    anchorBottom.Margin = new Thickness(x, anchorBottom.Margin.Top, anchorBottom.Margin.Right, anchorBottom.Margin.Bottom);
            //    //border.Width = Clamp(border.Width - x - posLeft, 50, totalWidth);
            //    //border.Height = Clamp(y - posTop, 50, totalHeight);
            //    //recSelection.Width = Clamp(totalWidth - x - posLeft, 50, totalWidth);
            //    //recSelection.Height = Clamp(y - posTop, 50, totalHeight);
            //    text2.Text = $"new width = {(int)border.Width}, new height = {(int)border.Height}";
            //}
        }
        private void anchorTop_MouseMove(object sender, MouseEventArgs e)
        {
            //if (isDragging)
            //{
            //    //var x = Clamp(e.GetPosition(gridChild).X, 0, totalWidth - 50);
            //    var y = Clamp(e.GetPosition(gridChild).Y, 0, totalHeight - 50);
            //    //var posLeft = border.Margin.Left;
            //    var posTop = border.Margin.Top;
            //    border.Margin = new Thickness(border.Margin.Left, y, border.Margin.Right, border.Margin.Bottom);
            //    anchorLeft.Margin = new Thickness(anchorLeft.Margin.Left, y, anchorLeft.Margin.Right, anchorLeft.Margin.Bottom);
            //    anchorTop.Margin = new Thickness(anchorTop.Margin.Left, y, anchorTop.Margin.Right, anchorTop.Margin.Bottom);
            //    anchorRight.Margin = new Thickness(anchorRight.Margin.Left, y, anchorRight.Margin.Right, anchorRight.Margin.Bottom);
            //    anchorBottom.Margin = new Thickness(anchorBottom.Margin.Left, y, anchorBottom.Margin.Right, anchorBottom.Margin.Bottom);
            //    //border.Width = Clamp(border.Width - x - posLeft, 50, totalWidth);
            //    //border.Height = Clamp(y - posTop, 50, totalHeight);
            //    //recSelection.Width = Clamp(totalWidth - x - posLeft, 50, totalWidth);
            //    //recSelection.Height = Clamp(y - posTop, 50, totalHeight);
            //    text2.Text = $"new width = {(int)border.Width}, new height = {(int)border.Height}";
            //}
        }
    }
}
