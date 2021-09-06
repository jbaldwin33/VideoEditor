using System;
using System.Windows;
using System.Windows.Threading;
using MVVMFramework.Localization;

namespace VideoEditorUi.Utilities
{
    public static class GlobalExceptionHandler
    {
        public static void Current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(UnravelException(e.Exception), new UnhandledExceptionTranslatable(), MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public static void DispatcherOnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(UnravelException(e.Exception), new UnhandledExceptionTranslatable(), MessageBoxButton.OK, MessageBoxImage.Error);
        }

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
