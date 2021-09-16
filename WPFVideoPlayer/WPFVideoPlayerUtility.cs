using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WPFVideoPlayer
{
    public static class VideoWindowUtility
    {
        #region dll imports

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32")]
        private static extern IntPtr SetParent(IntPtr hWnd, IntPtr hWndParent);

        [DllImport("user32")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, int uFlags);

        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        private const int SWP_NOZORDER = 0x0004;
        private const int SWP_NOACTIVATE = 0x0010;
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        #endregion

        public static void DoWindowStuff(Process process, Panel panel)
        {
            SetParent(process.MainWindowHandle, panel.Handle);
            SetWindowLong(process.MainWindowHandle, GWL_EXSTYLE, GetWindowLong(process.MainWindowHandle, GWL_EXSTYLE) | WS_EX_NOACTIVATE);
            ResizeEmbeddedApp(process, panel);
        }

        private static void ResizeEmbeddedApp(Process process, Panel panel)
        {
            if (process == null)
                return;
            SetWindowPos(process.MainWindowHandle, IntPtr.Zero, 80, 0, panel.ClientSize.Width, panel.ClientSize.Height, SWP_NOZORDER | SWP_NOACTIVATE);
        }
    }
}
