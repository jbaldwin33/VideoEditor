using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MVVMFramework.Localization;
using VideoEditorUi.ViewModels;

namespace VideoEditorUi.Views
{
    public static class ControlMethods
    {
        public static void ImagePanel_Drop(DragEventArgs e, Action<string[]> callback)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files == null || !files.Any(f => FormatTypeViewModel.IsVideoFile(Path.GetExtension(f).Remove(0, 1))))
                MessageBox.Show(new OnlyVideoFilesTranslatable(), new InformationLabelTranslatable(), MessageBoxButton.OK, MessageBoxImage.Information);
            else
                callback?.Invoke(files);
        }
    }
}
