using System;
using System.Windows;
using Microsoft.Win32;
using MVVMFramework.Localization;
using MVVMFramework.ViewModels;
using MVVMFramework.ViewNavigator;
using VideoEditorUi.Services;
using VideoEditorUi.Utilities;
using VideoUtilities;

namespace VideoEditorUi.ViewModels
{
    public class ReverseViewModel : EditorViewModel
    {
        public override string Name => new ReverserTranslatable();
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

        public ReverseViewModel(IUtilityClass utilityClass, IVideoEditorService editorService) : base(utilityClass, editorService)
        {

        }

        public override void OnUnloaded()
        {
            FileLoaded = false;
            base.OnUnloaded();
        }

        public override void Initialize()
        {
            WithSlider = false;
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
            GetDetailsEvent(openFileDialog.FileName);
            OpenEvent(openFileDialog.FileName);
            FileLoaded = true;
        }

        private void ReverseCommandExecute()
        {
            var messageArgs = new MessageBoxEventArgs(new ReverseVideoMessageTranslatable(), MessageBoxEventArgs.MessageTypeEnum.Warning, MessageBoxButton.YesNo, MessageBoxImage.Warning);
            ShowMessage(messageArgs);
            if (messageArgs.Result == MessageBoxResult.No)
                return;

            var args = new ReverserArgs(InputPath);
            Setup(false, false, args, Reverser_TrimFinished, Reverser_ReverseFinished);
            Execute(StageEnum.Pre, null);
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
            UtilityClass.CloseChildWindow(false);
            Setup(false, false, null, null, null, (int)e.Argument);
            Execute(StageEnum.Primary, new ReversingSectionsLabelTranslatable());
        }

        private void Reverser_ReverseFinished(object sender, EventArgs e)
        {
            UtilityClass.CloseChildWindow(false);
            Setup(false, false, null, null, null);
            Execute(StageEnum.Secondary, null);
        }

        public override void CleanUp(bool isError)
        {
            if (!isError)
            {
                InputPath = string.Empty;
                FileLoaded = false;
            }
            base.CleanUp(isError);
        }
    }
}