using System;
using System.Windows;
using System.Windows.Threading;
using MVVMFrameworkNet472.Localization;

namespace VideoEditorUi.Utilities
{
    public static class GlobalExceptionHandler
    {

        public static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.ExceptionObject.ToString(), new UnhandledExceptionTranslatable(), MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public static string UnravelException(Exception ex)
        {
            return ex == null 
                ? string.Empty 
                : $"{ex.Message}\n{UnravelException(ex.InnerException)}";
        }
    }
}
