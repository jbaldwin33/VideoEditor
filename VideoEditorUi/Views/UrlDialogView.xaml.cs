using MVVMFramework.Localization;
using MVVMFramework.ViewModels;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using VideoEditorUi.ViewModels;

namespace VideoEditorUi.Views
{
    /// <summary>
    /// Interaction logic for UrlDialogView.xaml
    /// </summary>
    public partial class UrlDialogView : Window
    {
        private DownloaderViewModel viewModel;
        public UrlDialogView()
        {
            InitializeComponent();
            Title = new AddUrlLabelTranslatable();
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Owner = Application.Current.MainWindow;
            Loaded += UrlDialogView_Loaded;
        }

        public void Initialize()
        {
            viewModel = DataContext as DownloaderViewModel;
        }

        private void UrlDialogView_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
        }

        private void ButtonBase_AddClick(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(inputText.Text))
                viewModel.AddUrl?.Invoke();
            else
                MessageBox.Show(new TextCannotBeEmptyTranslatable(), new InformationLabelTranslatable(), MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ButtonBase_CancelClick(object sender, RoutedEventArgs e) => DialogResult = false;


        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    }
}
