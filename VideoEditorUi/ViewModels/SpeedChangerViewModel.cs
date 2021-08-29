using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using MVVMFramework;
using MVVMFramework.ViewModels;
using MVVMFramework.ViewNavigator;
using VideoEditorUi.Utilities;
using VideoUtilities;
using VideoUtilities.Enums;

namespace VideoEditorUi.ViewModels
{
    public class SpeedChangerViewModel : ViewModel
    {
        private CSVideoPlayer.VideoPlayerWPF player;
        private bool changeSpeed;
        private double currentSpeed;
        private string speedLabel;
        private string inputPath;
        private bool fileLoaded;
        private bool canFormat;
        private int flipScale;
        private int rotateNumber;
        private RelayCommand flipCommand;
        private RelayCommand rotateCommand;
        private RelayCommand selectFileCommand;
        private RelayCommand formatCommand;
        private ProgressBarViewModel progressBarViewModel;
        private VideoSpeedChanger formatter;

        public CSVideoPlayer.VideoPlayerWPF Player
        {
            get => player;
            set => SetProperty(ref player, value);
        }

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
                CanFormat = value != 1;
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

        public bool FileLoaded
        {
            get => fileLoaded;
            set => SetProperty(ref fileLoaded, value);
        }
        public bool CanFormat
        {
            get => canFormat;
            set => SetProperty(ref canFormat, value);
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

        public ProgressBarViewModel ProgressBarViewModel
        {
            get => progressBarViewModel;
            set => SetProperty(ref progressBarViewModel, value);
        }

        public Slider SpeedSlider { get; set; }
        public StackPanel VideoStackPanel { get; set; }


        public RelayCommand SelectFileCommand => selectFileCommand ?? (selectFileCommand = new RelayCommand(SelectFileCommandExecute, () => true));
        public RelayCommand FormatCommand => formatCommand ?? (formatCommand = new RelayCommand(FormatCommandExecute, FormatCommandCanExecute));
        public RelayCommand FlipCommand => flipCommand ?? (flipCommand = new RelayCommand(FlipCommandExecute, () => FileLoaded));
        public RelayCommand RotateCommand => rotateCommand ?? (rotateCommand = new RelayCommand(RotateCommandExecute, () => FileLoaded));

        public string FormatLabel => Translatables.FormatLabel;
        public string SelectFileLabel => Translatables.SelectFileLabel;
        public string NoFileLabel => Translatables.NoFileSelected;
        public string FlipLabel => Translatables.FlipLabel;
        public string RotateLabel => Translatables.RotateLabel;
        public string VideoSpeedLabel => Translatables.VideoSpeedLabel;

        public SpeedChangerViewModel() { }

        public override void OnLoaded()
        {
            FlipScale = 1;
            SpeedSlider.ValueChanged += SpeedSlider_ValueChanged;
            SpeedSlider.Value = 1;
            SpeedLabel = "1x";
            base.OnLoaded();
        }

        public override void OnUnloaded()
        {
            UtilityClass.ClosePlayer(player);
            FileLoaded = false;
            SpeedSlider.ValueChanged -= SpeedSlider_ValueChanged;
            base.OnUnloaded();
        }

        private bool FormatCommandCanExecute() => FileLoaded && CanFormat;

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
            player.Open(new Uri(openFileDialog.FileName));
            FileLoaded = true;
        }

        private void FormatCommandExecute()
        {
            formatter = new VideoSpeedChanger(InputPath, CurrentSpeed, ConvertToEnum());
            formatter.StartedDownload += Converter_DownloadStarted;
            formatter.ProgressDownload += Converter_ProgressDownload;
            formatter.FinishedDownload += Converter_FinishedDownload;
            formatter.ErrorDownload += Converter_ErrorDownload;
            ProgressBarViewModel = new ProgressBarViewModel();
            ProgressBarViewModel.OnCancelledHandler += (sender, args) =>
            {
                try
                {
                    formatter.CancelOperation(string.Empty);
                }
                catch (Exception ex)
                {
                    ShowMessage(new MessageBoxEventArgs(ex.Message, MessageBoxEventArgs.MessageTypeEnum.Error, MessageBoxButton.OK, MessageBoxImage.Error));
                }
            };
            Task.Run(() => formatter.ChangeSpeed());
            Navigator.Instance.OpenChildWindow.Execute(ProgressBarViewModel);
        }

        private void FlipCommandExecute()
        {
            if (FlipScale < 0)
                FlipScale = 1;
            else
                FlipScale = -1;

            var scaleTransform = new ScaleTransform { ScaleX = FlipScale };
            var rotateTransform = new RotateTransform(RotateNumber * FlipScale);
            var transformGroup = new TransformGroup();
            transformGroup.Children.Add(rotateTransform);
            transformGroup.Children.Add(scaleTransform);
            VideoStackPanel.RenderTransformOrigin = new Point(0.5, 0.5);
            VideoStackPanel.LayoutTransform = transformGroup;
        }

        private void RotateCommandExecute()
        {
            RotateNumber = (RotateNumber + 90) % 360;

            var scaleTransform = new ScaleTransform { ScaleX = FlipScale };
            var rotateTransform = new RotateTransform(RotateNumber * FlipScale);
            var transformGroup = new TransformGroup();
            transformGroup.Children.Add(rotateTransform);
            transformGroup.Children.Add(scaleTransform);
            VideoStackPanel.RenderTransformOrigin = new Point(0.5, 0.5);
            VideoStackPanel.LayoutTransform = transformGroup;
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
            }
        }

        private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => CurrentSpeed = e.NewValue;

        private void Converter_DownloadStarted(object sender, DownloadStartedEventArgs e) => ProgressBarViewModel.UpdateLabel(e.Label);

        private void Converter_ProgressDownload(object sender, ProgressEventArgs e)
        {
            if (e.Percentage > ProgressBarViewModel.ProgressBarCollection[e.ProcessIndex].ProgressValue)
                ProgressBarViewModel.UpdateProgressValue(e.Percentage);
        }

        private void Converter_FinishedDownload(object sender, FinishedEventArgs e)
        {
            CleanUp();
            var message = e.Cancelled
                ? $"{Translatables.OperationCancelled} {e.Message}"
                : Translatables.VideoSpeedSuccessfullyChanged;
            ShowMessage(new MessageBoxEventArgs(message, MessageBoxEventArgs.MessageTypeEnum.Information, MessageBoxButton.OK, MessageBoxImage.Information));
        }

        private void Converter_ErrorDownload(object sender, ProgressEventArgs e)
        {
            Navigator.Instance.CloseChildWindow.Execute(false);
            ShowMessage(new MessageBoxEventArgs($"{Translatables.ErrorOccurred}\n\n{e.Error}", MessageBoxEventArgs.MessageTypeEnum.Error, MessageBoxButton.OK, MessageBoxImage.Error));
        }

        private void CleanUp()
        {
            Navigator.Instance.CloseChildWindow.Execute(false);
            ChangeSpeed = false;
            CurrentSpeed = 1;
            Application.Current.Dispatcher.Invoke(() => SpeedSlider.Value = 1);
            FileLoaded = false;
        }
    }
}