using MVVMFramework.ViewNavigator;
using MVVMFramework.Views;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VideoEditorUi.ViewModels;

namespace VideoEditorUi.Views
{
    /// <summary>
    /// Interaction logic for ResizeView.xaml
    /// </summary>
    public partial class ResizeView : ViewBaseControl
    {
        private bool isDragging = false;
        private double totalWidth;
        private double totalHeight;
        private ResizerViewModel viewModel;
        public ResizeView()
        {
            InitializeComponent();
            Utilities.UtilityClass.InitializePlayer(player);
            viewModel = Navigator.Instance.CurrentViewModel as ResizerViewModel;
            viewModel.Player = player;
            //gridChild.Width = ctlImage.Width;
            //gridChild.Height = ctlImage.Height;
        }

        private void ctlImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (isDragging == false)
            {
                isDragging = true;
                Mouse.Capture(sender as System.Windows.Shapes.Rectangle);
            }
        }

        private void ctlImage_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
                Mouse.Capture(null);
            }
        }

        private void ctlImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                var x = e.GetPosition(gridChild).X;
                var y = e.GetPosition(gridChild).Y;
                var posLeft = recSelection.Margin.Left;
                var posTop = recSelection.Margin.Top;
                recSelection.Margin = new Thickness(0, 0, 0, 0);
                recSelection.Width = Clamp(x - posLeft, totalWidth);
                recSelection.Height = Clamp(y - posTop, totalHeight);
                var ar = $"{(int)recSelection.Width / GCD((int)recSelection.Width, (int)recSelection.Height)}:{(int)recSelection.Height / GCD((int)recSelection.Width, (int)recSelection.Height)}";
                text2.Text = $"new width = {recSelection.Width:000.00}, new height = {recSelection.Height:000.00}, aspect ratio = {ar}";
            }
        }

        private void Grid_OnDrop(object sender, DragEventArgs e) => ControlMethods.ImagePanel_Drop(e, viewModel.DragFiles);
        private void OnDragEnter(object sender, DragEventArgs e) => ControlMethods.SetBackgroundBrush(sender as Label, false);

        private void OnPreviewDragLeave(object sender, DragEventArgs e) => ControlMethods.SetBackgroundBrush(sender as Label, true);

        private void player_MediaOpened(object sender, CSVideoPlayer.MediaOpenedEventArgs e)
        {
            var width = double.Parse(player.mediaProperties.Streams.Stream[0].Width);
            var height = double.Parse(player.mediaProperties.Streams.Stream[0].Height);
            player.Width = width;
            player.Height = height;
            var ratio = width / height;
            totalWidth = ratio * 250;
            totalHeight = 250;
            gridChild.Width = recSelection.Width = totalWidth;
            gridChild.Height = recSelection.Height = 250;

            var ar = string.Format("{0}:{1}", width / GCD((int)width, (int)height), height / GCD((int)width, (int)height));

            text1.Text = $"width = {width}, height = {height}, aspect ratio = {ar}";
        }

        private double Clamp(double val1, double max) => val1 > max ? max : val1;

        private int GCD(int a, int b)
        {
            int remainder;
            while (b != 0)
            {
                remainder = a % b;
                a = b;
                b = remainder;
            }

            return a;
        }
    }
}