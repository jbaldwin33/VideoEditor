using Microsoft.Win32;
using MVVMFrameworkNet472.Localization;
using MVVMFrameworkNet472.ViewModels;
using System;
using System.Windows;
using VideoEditorUi.Views;
using VideoUtilities;

namespace VideoEditorUi.ViewModels
{
    public class ResizerViewModel : EditorViewModel
    {
        #region Fields and props

        private string inputPath;
        private bool canCrop;
        private string oldSize;
        private string newSize;
        private string position;
        private string aspectRatio;
        private CropClass cropClass;
        private RelayCommand seekBackCommand;
        private RelayCommand playCommand;
        private RelayCommand seekForwardCommand;
        private RelayCommand selectFileCommand;
        private RelayCommand cropCommand;
        private RelayCommand openCropWindowCommand;

        public string InputPath
        {
            get => inputPath;
            set => SetProperty(ref inputPath, value);
        }

        public bool CanCrop
        {
            get => canCrop;
            set => SetProperty(ref canCrop, value);
        }

        public CropClass CropClass
        {
            get => cropClass;
            set => SetProperty(ref cropClass, value);
        }

        public string OldSize
        {
            get => oldSize;
            set => SetProperty(ref oldSize, value);
        }

        public string NewSize
        {
            get => newSize;
            set => SetProperty(ref newSize, value);
        }

        public string Position
        {
            get => position;
            set => SetProperty(ref position, value);
        }

        public string AspectRatio
        {
            get => aspectRatio;
            set => SetProperty(ref aspectRatio, value);
        }


        #endregion

        #region Commands

        public RelayCommand SeekBackCommand => seekBackCommand ?? (seekBackCommand = new RelayCommand(SeekBackCommandExecute, () => FileLoaded));
        public RelayCommand SeekForwardCommand => seekForwardCommand ?? (seekForwardCommand = new RelayCommand(SeekForwardCommandExecute, () => FileLoaded));
        public RelayCommand PlayCommand => playCommand ?? (playCommand = new RelayCommand(PlayCommandExecute, () => FileLoaded));
        public RelayCommand SelectFileCommand => selectFileCommand ?? (selectFileCommand = new RelayCommand(SelectFileCommandExecute, () => true));
        public RelayCommand CropCommand => cropCommand ?? (cropCommand = new RelayCommand(CropCommandExecute, () => CropClass != null));
        public RelayCommand OpenCropWindowCommand => openCropWindowCommand ?? (openCropWindowCommand = new RelayCommand(OpenCropWindowCommandExecute, () => FileLoaded));



        #endregion

        #region Labels

        public string DragFileLabel => new DragFileTranslatable();
        public string CropLabel => new CropTranslatable();
        public string OpenCropWindowLabel => new OpenCropWindowTranslatable();

        #endregion

        public override void OnUnloaded()
        {
            ClosePlayerEvent?.Invoke();
            FileLoaded = false;
            base.OnUnloaded();
        }

        public override void Initialize()
        {
            OldSize = $"Old size: 0x0";
            NewSize = $"New size: 0x0";
            Position = $"Starting position: (0,0)";
        }

        private void SeekBackCommandExecute() => SeekEvent(-5000);
        private void SeekForwardCommandExecute() => SeekEvent(5000);

        private void SelectFileCommandExecute()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "All Video Files|*.png;*.wmv;*.avi;*.mpg;*.mpeg;*.mp4;*.mov;*.m4a;*.mkv;*.ts;*.WMV;*.AVI;*.MPG;*.MPEG;*.MP4;*.MOV;*.M4A;*.MKV;*.TS",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
            };

            if (openFileDialog.ShowDialog() == false)
                return;

            InputPath = openFileDialog.FileName;
            FileLoaded = true;

            var cropWindow = new CropWindow(openFileDialog.FileName, this);
            cropWindow.Show();
        }

        private void CropCommandExecute()
        {
            var args = new VideoCropperArgs(InputPath, CropClass);
            Setup(true, false, args, null, null);
            Execute(StageEnum.Primary, new CroppingLabelTranslatable());
        }

        private void OpenCropWindowCommandExecute()
        {
            var cropWindow = new CropWindow(InputPath, this);
            cropWindow.Show();
        }


        protected override void FinishedDownload(object sender, FinishedEventArgs e)
        {
            base.FinishedDownload(sender, e);
            var message = e.Cancelled
                ? $"{new OperationCancelledTranslatable()} {e.Message}"
                : new VideoSuccessfullyCroppedTranslatable();
            ShowMessage(new MessageBoxEventArgs(message, MessageBoxEventArgs.MessageTypeEnum.Information, MessageBoxButton.OK, MessageBoxImage.Information));
        }

        public override void CleanUp(bool isError)
        {
            if (!isError)
            {
                FileLoaded = false;
                InputPath = string.Empty;
                CanCrop = false;
                OldSize = $"Old size: 0x0";
                NewSize = $"New size: 0x0";
                Position = $"Starting position: (0,0)";
                CropClass = null;
            }
            base.CleanUp(isError);
        }
    }
}
