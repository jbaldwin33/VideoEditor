using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using MVVMFramework.Localization;
using MVVMFramework.ViewModels;
using VideoUtilities;

namespace VideoEditorUi.ViewModels
{
    public class SpeedChangerViewModel : EditorViewModel
    {
        #region Fields and props

        private bool changeSpeed;
        private double currentSpeed;
        private string speedLabel;
        private string inputPath;
        private int flipScale;
        private int rotateNumber;
        private RelayCommand flipCommand;
        private RelayCommand rotateCommand;
        private RelayCommand selectFileCommand;
        private RelayCommand formatCommand;

        public bool ChangeSpeed
        {
            get => changeSpeed;
            set => SetProperty(ref changeSpeed, value);
        }

        public double CurrentSpeed
        {
            get => currentSpeed;
            set
            {
                SetProperty(ref currentSpeed, value);
                SpeedLabel = $"{value}x";
            }
        }

        public string SpeedLabel
        {
            get => speedLabel;
            set => SetProperty(ref speedLabel, value);
        }

        public string InputPath
        {
            get => inputPath;
            set => SetProperty(ref inputPath, value);
        }

        public int FlipScale
        {
            get => flipScale;
            set => SetProperty(ref flipScale, value);
        }

        public int RotateNumber
        {
            get => rotateNumber;
            set => SetProperty(ref rotateNumber, value);
        }

        #endregion

        public Action<double> UpdateSliderEvent;
        public Action<int, int> UpdateStackPanelEvent;

        #region Commands

        public RelayCommand SelectFileCommand => selectFileCommand ?? (selectFileCommand = new RelayCommand(SelectFileCommandExecute, () => true));
        public RelayCommand FormatCommand => formatCommand ?? (formatCommand = new RelayCommand(FormatCommandExecute, FormatCommandCanExecute));
        public RelayCommand FlipCommand => flipCommand ?? (flipCommand = new RelayCommand(FlipCommandExecute, () => FileLoaded));
        public RelayCommand RotateCommand => rotateCommand ?? (rotateCommand = new RelayCommand(RotateCommandExecute, () => FileLoaded));

        #endregion

        #region Labels

        public string FormatLabel => new FormatLabelTranslatable();
        public string VideoSpeedLabel => new VideoSpeedLabelTranslatable();
        public string DragFileLabel => new DragFileTranslatable();

        #endregion

        public override void OnUnloaded()
        {
            ClosePlayerEvent?.Invoke();
            FileLoaded = false;
            base.OnUnloaded();
        }

        public override void Initialize()
        {
            WithSlider = false;
            FlipScale = 1;
            SpeedLabel = "1x";
        }

        private bool FormatCommandCanExecute() => FileLoaded && (RotateNumber != 0 || FlipScale != 1 || CurrentSpeed != 1);

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

        private void FormatCommandExecute()
        {
            var args = new SpeedChangerArgs(InputPath, CurrentSpeed, ConvertToEnum());
            Setup(true, false, args, null, null);
            Execute(StageEnum.Primary, new ChangingLabelTranslatable());
        }

        private void FlipCommandExecute()
        {
            FlipScale = FlipScale < 0 ? 1 : -1;
            UpdateStackPanelEvent(RotateNumber, FlipScale);
        }

        private void RotateCommandExecute()
        {
            RotateNumber = (RotateNumber + 90) % 360;
            UpdateStackPanelEvent(RotateNumber, FlipScale);
        }

        private Enums.ScaleRotate ConvertToEnum()
        {
            var sr = (FlipScale, RotateNumber);
            switch (sr)
            {
                case var tuple when tuple.FlipScale == 1 && tuple.RotateNumber == 0: return Enums.ScaleRotate.NoSNoR;
                case var tuple when tuple.FlipScale == 1 && tuple.RotateNumber == 90: return Enums.ScaleRotate.NoS90R;
                case var tuple when tuple.FlipScale == 1 && tuple.RotateNumber == 180: return Enums.ScaleRotate.NoS180R;
                case var tuple when tuple.FlipScale == 1 && tuple.RotateNumber == 270: return Enums.ScaleRotate.NoS270R;
                case var tuple when tuple.FlipScale == -1 && tuple.RotateNumber == 0: return Enums.ScaleRotate.SNoR;
                case var tuple when tuple.FlipScale == -1 && tuple.RotateNumber == 90: return Enums.ScaleRotate.S90R;
                case var tuple when tuple.FlipScale == -1 && tuple.RotateNumber == 180: return Enums.ScaleRotate.S180R;
                case var tuple when tuple.FlipScale == -1 && tuple.RotateNumber == 270: return Enums.ScaleRotate.S270R;
                default: throw new ArgumentOutOfRangeException(nameof(sr), sr, null);
            };
        }


        protected override void FinishedDownload(object sender, FinishedEventArgs e)
        {
            base.FinishedDownload(sender, e);
            var message = e.Cancelled
                ? $"{new OperationCancelledTranslatable()} {e.Message}"
                : new VideoSpeedSuccessfullyChangedTranslatable();
            ShowMessage(new MessageBoxEventArgs(message, MessageBoxEventArgs.MessageTypeEnum.Information, MessageBoxButton.OK, MessageBoxImage.Information));
        }

        public override void CleanUp(bool isError)
        {
            if (!isError)
            {
                ChangeSpeed = false;
                CurrentSpeed = 1;
                FlipScale = 1;
                RotateNumber = 0;
                Application.Current.Dispatcher.Invoke(() => UpdateSliderEvent(1));
                FileLoaded = false;
            }
            base.CleanUp(isError);
        }
    }
}