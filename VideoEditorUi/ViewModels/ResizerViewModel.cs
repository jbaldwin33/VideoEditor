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
        #region Fields and props

        private string inputPath;
        private bool canCrop;
        private CropClass cropClass;
        private RelayCommand selectFileCommand;
        private RelayCommand cropCommand;
        private RelayCommand openCropWindowCommand;
        private Window cropWindow;

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

        private string originalWidthHeight;
        private string newWidthHeight;
        private string position;

        public string OriginalWidthHeight
        {
            get => originalWidthHeight;
            set => SetProperty(ref originalWidthHeight, value);
        }

        public string NewWidthHeight
        {
            get => newWidthHeight;
            set => SetProperty(ref newWidthHeight, value);
        }

        public string Position
        {
            get => position;
            set => SetProperty(ref position, value);
        }




        #endregion

        #region Commands

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

            cropWindow = new CropWindow(openFileDialog.FileName, this);
            cropWindow.Show();
        }

        private void CropCommandExecute()
        {
            VideoEditor = new VideoCropper(InputPath, CropClass.Width, CropClass.Height, CropClass.X, CropClass.Y);
            Setup(true);
            Execute(StageEnum.Primary, new CroppingLabelTranslatable());
        }

        private void OpenCropWindowCommandExecute()
        {
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

    public class CropClass
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
    }
}
