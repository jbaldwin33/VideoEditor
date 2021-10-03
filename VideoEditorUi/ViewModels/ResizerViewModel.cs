using Microsoft.Win32;
using MVVMFramework.Localization;
using MVVMFramework.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using VideoEditorUi.Utilities;
using VideoEditorUi.Views;
using VideoUtilities;

namespace VideoEditorUi.ViewModels
{
    public class ResizerViewModel : EditorViewModel
    {
        private string inputPath;
        private RelayCommand selectFileCommand;

        public string InputPath
        {
            get => inputPath;
            set => SetProperty(ref inputPath, value);
        }

        public RelayCommand SelectFileCommand => selectFileCommand ?? (selectFileCommand = new RelayCommand(SelectFileCommandExecute, () => true));

        public override void OnUnloaded()
        {
            UtilityClass.ClosePlayer(Player);
            FileLoaded = false;
            base.OnUnloaded();
        }

        protected override void Initialize()
        {

        }

        private void SelectFileCommandExecute()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "All Video Files|*.wmv;*.avi;*.mpg;*.mpeg;*.mp4;*.mov;*.m4a;*.mkv;*.ts;*.WMV;*.AVI;*.MPG;*.MPEG;*.MP4;*.MOV;*.M4A;*.MKV;*.TS",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
            };

            if (openFileDialog.ShowDialog() == false)
                return;

            InputPath = openFileDialog.FileName;
            UtilityClass.GetDetails(Player, openFileDialog.FileName);
            Player.Open(new Uri(openFileDialog.FileName));
            FileLoaded = true;

            var cropWindow = new CropWindow(openFileDialog.FileName);
            cropWindow.Show();
        }

        protected override void FinishedDownload(object sender, FinishedEventArgs e)
        {
            base.FinishedDownload(sender, e);
            var message = e.Cancelled
                ? $"{new OperationCancelledTranslatable()} {e.Message}"
                : new VideoSuccessfullyResizedTranslatable();
            ShowMessage(new MessageBoxEventArgs(message, MessageBoxEventArgs.MessageTypeEnum.Information, MessageBoxButton.OK, MessageBoxImage.Information));
        }

        protected override void CleanUp()
        {
            FileLoaded = false;
            base.CleanUp();
        }
    }
}
