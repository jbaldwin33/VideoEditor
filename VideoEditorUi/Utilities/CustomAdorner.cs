using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using VideoEditorUi.ViewModels;

namespace VideoEditorUi.Utilities
{
    public class CustomAdorner : Adorner
    {
        private double angle = 0.0;
        public double TopPos, LeftPos;
        private System.Drawing.Point transformOrigin = new System.Drawing.Point(0, 0);
        public Border childElement;
        private VisualCollection visualChildren;
        public Thumb leftTop, rightTop, leftBottom, rightBottom;
        private bool dragStarted = false;
        private bool isHorizontalDrag = false;
        private double origX;
        private double maxX;
        private double origY;
        private double maxY;
        private ResizerViewModel viewModel;

        public CustomAdorner(UIElement element, ResizerViewModel vm) : base(element)
        {
            viewModel = vm;
            visualChildren = new VisualCollection(this);
            childElement = element as Border;
            origX = Canvas.GetLeft(childElement);
            maxX = origX + childElement.MaxWidth - 50;
            origY = Canvas.GetTop(childElement);
            maxY = origY + childElement.MaxHeight - 50;
            CreateThumbPart(ref leftTop);
            leftTop.DragDelta += (sender, e) =>
            {
                var hor = e.HorizontalChange;
                var vert = e.VerticalChange;
                //if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                //{
                //    if (dragStarted) isHorizontalDrag = Math.Abs(hor) > Math.Abs(vert);
                //    if (isHorizontalDrag) vert = hor; else hor = vert;
                //}
                ResizeX(hor);
                ResizeY(vert);
                dragStarted = false;
                e.Handled = true;
            };
            CreateThumbPart(ref rightTop);
            rightTop.DragDelta += (sender, e) =>
            {
                var hor = e.HorizontalChange;
                var vert = e.VerticalChange;
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                {
                    if (dragStarted) isHorizontalDrag = Math.Abs(hor) > Math.Abs(vert);
                    if (isHorizontalDrag) vert = -hor; else hor = -vert;
                }
                ResizeWidth(hor);
                ResizeY(vert);
                dragStarted = false;
                e.Handled = true;
            };
            CreateThumbPart(ref leftBottom);
            leftBottom.DragDelta += (sender, e) =>
            {
                var hor = e.HorizontalChange;
                var vert = e.VerticalChange;
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                {
                    if (dragStarted) isHorizontalDrag = Math.Abs(hor) > Math.Abs(vert);
                    if (isHorizontalDrag) vert = -hor; else hor = -vert;
                }
                ResizeX(hor);
                ResizeHeight(vert);
                dragStarted = false;
                e.Handled = true;
            };
            CreateThumbPart(ref rightBottom);
            rightBottom.DragDelta += (sender, e) =>
            {
                var hor = e.HorizontalChange;
                var vert = e.VerticalChange;
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                {
                    if (dragStarted) isHorizontalDrag = Math.Abs(hor) > Math.Abs(vert);
                    if (isHorizontalDrag) vert = hor; else hor = vert;
                }
                ResizeWidth(hor);
                ResizeHeight(vert);
                dragStarted = false;
                e.Handled = true;
            };
        }
        public void CreateThumbPart(ref Thumb cornerThumb)
        {
            var resource = FindResource("ThumbStyle") as Style;
            cornerThumb = new Thumb { Width = 10, Height = 10, Background = Brushes.Black, Style = resource };
            cornerThumb.DragStarted += (object sender, DragStartedEventArgs e) => dragStarted = true;
            visualChildren.Add(cornerThumb);
        }

        private void ResizeWidth(double e)
        {
            var deltaHorizontal = Math.Min(-e, childElement.ActualWidth - childElement.MinWidth);
            TopPos = Clamp(Canvas.GetTop(childElement) - transformOrigin.X * deltaHorizontal * Math.Sin(angle), origY, maxY);
            LeftPos = Clamp(Canvas.GetLeft(childElement) + (deltaHorizontal * transformOrigin.X * (1 - Math.Cos(angle))), origX, maxX);
            Canvas.SetTop(childElement, TopPos);
            Canvas.SetLeft(childElement, LeftPos);
            childElement.Width = Clamp(childElement.Width - deltaHorizontal, 50, childElement.MaxWidth);
            viewModel.NewWidthHeight = $"new width and height = ({childElement.Width},{childElement.Height})";
            viewModel.Position = $"position = ({LeftPos},{TopPos})";
        }
        private void ResizeX(double e)
        {
            var deltaHorizontal = Math.Min(e, childElement.ActualWidth - childElement.MinWidth);
            TopPos = Clamp(Canvas.GetTop(childElement) + deltaHorizontal * Math.Sin(angle) - transformOrigin.X * deltaHorizontal * Math.Sin(angle), origY, maxY);
            LeftPos = Clamp(Canvas.GetLeft(childElement) + deltaHorizontal * Math.Cos(angle) + (transformOrigin.X * deltaHorizontal * (1 - Math.Cos(angle))), origX, maxX);
            Canvas.SetTop(childElement, TopPos);
            Canvas.SetLeft(childElement, LeftPos);
            childElement.Width = Clamp(childElement.Width - deltaHorizontal, 50, childElement.MaxWidth);
            viewModel.NewWidthHeight = $"new width and height = ({childElement.Width},{childElement.Height})";
            viewModel.Position = $"position = ({LeftPos},{TopPos})";
        }
        private void ResizeHeight(double e)
        {
            var deltaVertical = Math.Min(-e, childElement.ActualHeight - childElement.MinHeight);
            TopPos = Clamp(Canvas.GetTop(childElement) + (transformOrigin.Y * deltaVertical * (1 - Math.Cos(-angle))), origY, maxY);
            LeftPos = Clamp(Canvas.GetLeft(childElement) - deltaVertical * transformOrigin.Y * Math.Sin(-angle), origX, maxX);
            Canvas.SetTop(childElement, TopPos);
            Canvas.SetLeft(childElement, LeftPos);
            childElement.Height = Clamp(childElement.Height - deltaVertical, 50, childElement.MaxHeight);
            viewModel.NewWidthHeight = $"new width and height = ({childElement.Width},{childElement.Height})";
            viewModel.Position = $"position = ({LeftPos},{TopPos})";
        }
        private void ResizeY(double e)
        {
            var deltaVertical = Math.Min(e, childElement.ActualHeight - childElement.MinHeight);
            TopPos = Clamp(Canvas.GetTop(childElement) + deltaVertical * Math.Cos(-angle) + (transformOrigin.Y * deltaVertical * (1 - Math.Cos(-angle))), origY, maxY);
            LeftPos = Clamp(Canvas.GetLeft(childElement) + deltaVertical * Math.Sin(-angle) - (transformOrigin.Y * deltaVertical * Math.Sin(-angle)), origX, maxX);
            Canvas.SetTop(childElement, TopPos);
            Canvas.SetLeft(childElement, LeftPos);
            childElement.Height = Clamp(childElement.Height - deltaVertical, 50, childElement.MaxHeight);
            viewModel.NewWidthHeight = $"new width and height = ({childElement.Width},{childElement.Height})";
            viewModel.Position = $"position = ({LeftPos},{TopPos})";
        }
        //public void EnforceSize(FrameworkElement element)
        //{
        //    if (element.Width.Equals(Double.NaN))
        //        element.Width = element.DesiredSize.Width;
        //    if (element.Height.Equals(Double.NaN))
        //        element.Height = element.DesiredSize.Height;
        //    FrameworkElement parent = element.Parent as FrameworkElement;
        //    if (parent != null)
        //    {
        //        element.MaxHeight = parent.ActualHeight;
        //        element.MaxWidth = parent.ActualWidth;
        //    }
        //}
        protected override Size ArrangeOverride(Size finalSize)
        {
            base.ArrangeOverride(finalSize);
            var desireWidth = AdornedElement.DesiredSize.Width;
            var desireHeight = AdornedElement.DesiredSize.Height;
            var adornerWidth = DesiredSize.Width;
            var adornerHeight = DesiredSize.Height;
            leftTop.Arrange(new Rect(-adornerWidth / 2 + 5, -adornerHeight / 2 + 5, adornerWidth, adornerHeight));
            rightTop.Arrange(new Rect(desireWidth - adornerWidth / 2 - 5, -adornerHeight / 2 + 5, adornerWidth, adornerHeight));
            leftBottom.Arrange(new Rect(-adornerWidth / 2 + 5, desireHeight - adornerHeight / 2 - 5, adornerWidth, adornerHeight));
            rightBottom.Arrange(new Rect(desireWidth - adornerWidth / 2 - 5, desireHeight - adornerHeight / 2 - 5, adornerWidth, adornerHeight));
            return finalSize;
        }
        protected override int VisualChildrenCount => visualChildren.Count;
        protected override Visual GetVisualChild(int index) => visualChildren[index];
        //protected override void OnRender(DrawingContext drawingContext) => base.OnRender(drawingContext);

        private double Clamp(double val, double min, double max) => val > max ? max : val < min ? min : val;
    }
}
