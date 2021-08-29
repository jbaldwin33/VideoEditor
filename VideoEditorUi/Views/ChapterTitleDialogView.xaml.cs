using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using MVVMFramework.ViewModels;

namespace VideoEditorUi.Views
{
    /// <summary>
    /// Interaction logic for ChapterTitleDialog.xaml
    /// </summary>
    public partial class ChapterTitleDialogView : Window
    {
        public ChapterTitleDialogView(ViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
            Loaded += ChapterTitleDialogView_Loaded;
        }

        private void ChapterTitleDialogView_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e) => DialogResult = true;
        

        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    }
}
