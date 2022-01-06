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
    public class ResizeAdorner : Adorner
    {
        private readonly VisualCollection visualChildren;
        private readonly double origX;
        private readonly double maxX;
        private readonly double origY;
        private readonly double maxY;
        private readonly ResizerViewModel viewModel;
        private readonly double angle = 0.0;
        private readonly Thumb leftTop, rightTop, leftBottom, rightBottom;
        private double topPos;
        private double leftPos;
        private double childWidth;
        private double childHeight;
        private Point transformOrigin = new Point(0, 0);
        private Border childElement;
        private bool dragStarted = false;
        private bool isHorizontalDrag = false;

        public double TopPos
        {
            get => topPos;
            set
            {
                topPos = value;
                Canvas.SetTop(childElement, value);
                viewModel.Position = $"Position: ({LeftPos},{TopPos})";
            }
        }

        public double LeftPos
        {
            get => leftPos;
            set
            {
                leftPos = value;
                Canvas.SetLeft(childElement, value);
                viewModel.Position = $"Position: ({LeftPos},{TopPos})";
            }
        }

        public double ChildWidth
        {
            get => childWidth;
            set
            {
                childWidth = childElement.Width = value;
                viewModel.NewSize = $"New size: {childElement.Width}x{childElement.Height}";
            }
        }

        public double ChildHeight
        {
            get => childHeight;
            set
            {
                childHeight = childElement.Height = value;
                viewModel.NewSize = $"New size: {childElement.Width}x{childElement.Height}";
            }
        }

        public ResizeAdorner(UIElement element, ResizerViewModel vm) : base(element)
        {
            viewModel = vm;
            visualChildren = new VisualCollection(this);
            childElement = element as Border;
            origX = Canvas.GetLeft(childElement);
            maxX = origX + childElement.MaxWidth - 50;
            origY = Canvas.GetTop(childElement);
            maxY = origY + childElement.MaxHeight - 50;

            if (viewModel.CropClass != null)
                SetDimensions();

            CreateThumbPart(ref leftTop);
            leftTop.DragDelta += LeftTop_DragDelta;
            CreateThumbPart(ref rightTop);
            rightTop.DragDelta += RightTop_DragDelta;
            CreateThumbPart(ref leftBottom);
            leftBottom.DragDelta += LeftBottom_DragDelta;
            CreateThumbPart(ref rightBottom);
            rightBottom.DragDelta += RightBottom_DragDelta;
        }

        #region Events

        private void LeftTop_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var hor = e.HorizontalChange;
            var vert = e.VerticalChange;
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                if (dragStarted) isHorizontalDrag = Math.Abs(hor) > Math.Abs(vert);
                if (isHorizontalDrag) vert = hor; else hor = vert;
            }
            ResizeX(hor);
            ResizeY(vert);
            dragStarted = false;
            e.Handled = true;
        }

        private void RightTop_DragDelta(object sender, DragDeltaEventArgs e)
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
        }

        private void LeftBottom_DragDelta(object sender, DragDeltaEventArgs e)
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
        }

        private void RightBottom_DragDelta(object sender, DragDeltaEventArgs e)
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
        }

        #endregion

        #region Resizing

        private void ResizeWidth(double e)
        {
            var deltaHorizontal = Math.Min(-e, childElement.ActualWidth - childElement.MinWidth);
            TopPos = Clamp(Canvas.GetTop(childElement) - transformOrigin.X * deltaHorizontal * Math.Sin(angle), origY, maxY);
            LeftPos = Clamp(Canvas.GetLeft(childElement) + (deltaHorizontal * transformOrigin.X * (1 - Math.Cos(angle))), origX, maxX);
            ChildWidth = Clamp(childWidth - deltaHorizontal, 50, childElement.MaxWidth);
        }

        private void ResizeX(double e)
        {
            var deltaHorizontal = Math.Min(e, childElement.ActualWidth - childElement.MinWidth);
            TopPos = Clamp(Canvas.GetTop(childElement) + deltaHorizontal * Math.Sin(angle) - transformOrigin.X * deltaHorizontal * Math.Sin(angle), origY, maxY);
            LeftPos = Clamp(Canvas.GetLeft(childElement) + deltaHorizontal * Math.Cos(angle) + (transformOrigin.X * deltaHorizontal * (1 - Math.Cos(angle))), origX, maxX);
            ChildWidth = Clamp(childWidth - deltaHorizontal, 50, childElement.MaxWidth);
        }

        private void ResizeHeight(double e)
        {
            var deltaVertical = Math.Min(-e, childElement.ActualHeight - childElement.MinHeight);
            TopPos = Clamp(Canvas.GetTop(childElement) + (transformOrigin.Y * deltaVertical * (1 - Math.Cos(-angle))), origY, maxY);
            LeftPos = Clamp(Canvas.GetLeft(childElement) - deltaVertical * transformOrigin.Y * Math.Sin(-angle), origX, maxX);
            ChildHeight = Clamp(childHeight - deltaVertical, 50, childElement.MaxHeight);
        }

        private void ResizeY(double e)
        {
            var deltaVertical = Math.Min(e, childElement.ActualHeight - childElement.MinHeight);
            TopPos = Clamp(Canvas.GetTop(childElement) + deltaVertical * Math.Cos(-angle) + (transformOrigin.Y * deltaVertical * (1 - Math.Cos(-angle))), origY, maxY);
            LeftPos = Clamp(Canvas.GetLeft(childElement) + deltaVertical * Math.Sin(-angle) - (transformOrigin.Y * deltaVertical * Math.Sin(-angle)), origX, maxX);
            ChildHeight = Clamp(childHeight - deltaVertical, 50, childElement.MaxHeight);
        }

        #endregion

        private void CreateThumbPart(ref Thumb cornerThumb)
        {
            var resource = FindResource("ThumbStyle") as Style;
            cornerThumb = new Thumb { Width = 10, Height = 10, Background = Brushes.Black, Style = resource };
            cornerThumb.DragStarted += (object sender, DragStartedEventArgs e) => dragStarted = true;
            visualChildren.Add(cornerThumb);
        }

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
        private double Clamp(double val, double min, double max) => val > max ? max : val < min ? min : val;

        private void SetDimensions()
        {
            ChildWidth = viewModel.CropClass.Width;
            ChildHeight = viewModel.CropClass.Height;
            LeftPos = viewModel.CropClass.X;
            TopPos = viewModel.CropClass.Y;
        }
    }
}
