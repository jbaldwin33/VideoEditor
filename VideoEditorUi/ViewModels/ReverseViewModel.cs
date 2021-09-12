using System;
using System.Windows;
using Microsoft.Win32;
using MVVMFramework.Localization;
using MVVMFramework.ViewModels;
using MVVMFramework.ViewNavigator;
using VideoUtilities;

namespace VideoEditorUi.ViewModels
{
    public class ReverseViewModel : EditorViewModel
    {
        #region Fields and props

        private string inputPath;
        private RelayCommand selectFileCommand;
        private RelayCommand reverseCommand;

        public string InputPath
        {
            get => inputPath;
            set => SetProperty(ref inputPath, value);
        }

        #endregion

        #region Commands

        public RelayCommand SelectFileCommand => selectFileCommand ?? (selectFileCommand = new RelayCommand(SelectFileCommandExecute, () => true));
        public RelayCommand ReverseCommand => reverseCommand ?? (reverseCommand = new RelayCommand(ReverseCommandExecute, () => FileLoaded));

        #endregion

        #region Labels

        public string ReverseLabel => new ReverseLabelTranslatable();
        public string SelectFileLabel => new SelectFileLabelTranslatable();
        public string DragFileLabel => new DragFileTranslatable();

        #endregion

        public override void OnUnloaded()
        {
            FileLoaded = false;
            base.OnUnloaded();
        }

        protected override void Initialize() { }

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
            Player.Open(new Uri(openFileDialog.FileName));
            FileLoaded = true;
        }

        private void ReverseCommandExecute()
        {
            var messageArgs = new MessageBoxEventArgs(new ReverseVideoMessageTranslatable(), MessageBoxEventArgs.MessageTypeEnum.Warning, MessageBoxButton.YesNo, MessageBoxImage.Warning);
            ShowMessage(messageArgs);
            if (messageArgs.Result == MessageBoxResult.No)
                return;
            VideoEditor = new VideoReverser(InputPath);
            VideoEditor.PreWorkFinished += Reverser_TrimFinished;
            VideoEditor.FirstWorkFinished += Reverser_ReverseFinished;
            Execute(false, StageEnum.Pre);
        }

        protected override void FinishedDownload(object sender, FinishedEventArgs e)
        {
            base.FinishedDownload(sender, e);
            var message = e.Cancelled
                ? $"{new OperationCancelledTranslatable()} {e.Message}"
                : new VideoSuccessfullyReversedTranslatable();
            ShowMessage(new MessageBoxEventArgs(message, MessageBoxEventArgs.MessageTypeEnum.Information, MessageBoxButton.OK, MessageBoxImage.Information));
        }

        private void Reverser_TrimFinished(object sender, PreWorkEventArgs e)
        {
            Navigator.Instance.CloseChildWindow.Execute(false);
            Execute(false, StageEnum.Primary, new ReversingSectionsLabelTranslatable(), (int)e.Argument);
        }

        private void Reverser_ReverseFinished(object sender, EventArgs e)
        {
            Navigator.Instance.CloseChildWindow.Execute(false);
            Execute(false, StageEnum.Secondary);
        }

        protected override void CleanUp()
        {
            InputPath = string.Empty;
            FileLoaded = false;
            base.CleanUp();
        }
    }
}