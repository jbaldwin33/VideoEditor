using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.RightsManagement;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MVVMFramework;
using MVVMFramework.ViewNavigator;
using MVVMFramework.ViewModels;
using VideoUtilities;
using static VideoUtilities.Enums.Enums;

namespace VideoEditorUi.ViewModels
{
    public class ConverterViewModel : ViewModel
    {
        private ObservableCollection<FormatTypeViewModel> formats;
        private FormatEnum formatType;
        private string filename;
        private RelayCommand selectFileCommand;
        private RelayCommand convertCommand;
        private RelayCommand flipCommand;
        private RelayCommand rotateCommand;
        private string sourceFolder;
        private string extension;
        private bool fileLoaded;
        private bool outputDifferentFormat;
        private VideoConverter converter;
        private decimal progressValue;
        private ProgressBarViewModel progressBarViewModel;
        private CSVideoPlayer.VideoPlayerWPF player;
        private int flipScale;
        private int rotateNumber;
        

        public ObservableCollection<FormatTypeViewModel> Formats
        {
            get => formats;
            set => SetProperty(ref formats, value);
        }

        public FormatEnum FormatType
        {
            get => formatType;
            set => SetProperty(ref formatType, value);
        }

        public string Filename
        {
            get => filename;
            set => SetProperty(ref filename, value);
        }

        public bool FileLoaded
        {
            get => fileLoaded;
            set => SetProperty(ref fileLoaded, value);
        }

        public bool OutputDifferentFormat
        {
            get => outputDifferentFormat;
            set => SetProperty(ref outputDifferentFormat, value);
        }

        public decimal ProgressValue
        {
            get => progressValue;
            set => SetProperty(ref progressValue, value);
        }

        public ProgressBarViewModel ProgressBarViewModel
        {
            get => progressBarViewModel;
            set => SetProperty(ref progressBarViewModel, value);
        }

        public CSVideoPlayer.VideoPlayerWPF Player
        {
            get => player;
            set => SetProperty(ref player, value);
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
        
        


        public RelayCommand SelectFileCommand => selectFileCommand ?? (selectFileCommand = new RelayCommand(SelectFileCommandExecute, () => true));
        public RelayCommand ConvertCommand => convertCommand ?? (convertCommand = new RelayCommand(ConvertCommandExecute, ConvertCommandCanExecute));
        public RelayCommand FlipCommand => flipCommand ?? (flipCommand = new RelayCommand(FlipCommandExecute, () => FileLoaded));
        public RelayCommand RotateCommand => rotateCommand ?? (rotateCommand = new RelayCommand(RotateCommandExecute, () => FileLoaded));

        public string SelectFileLabel => Translatables.SelectFileLabel;
        public string ConvertLabel => Translatables.ConvertLabel;
        public string FlipLabel => Translatables.FlipLabel;
        public string RotateLabel => Translatables.RotateLabel;
        public string OutputFormatLabel => Translatables.OutputFormatLabel;
        public string NoFileLabel => Translatables.NoFileSelected;
        public StackPanel VideoStackPanel { get; set; }
        public Action<double> SpeedChanged;

        public ConverterViewModel()
        {
            Formats = FormatTypeViewModel.CreateViewModels();
            FormatType = FormatEnum.avi;
            FlipScale = 1;
        }

        private bool ConvertCommandCanExecute() => FileLoaded;

        private void SelectFileCommandExecute()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "All Video Files|*.wmv;*.avi;*.mpg;*.mpeg;*.mp4;*.mov;*.m4a;*.mkv;*.ts;*.WMV;*.AVI;*.MPG;*.MPEG;*.MP4;*.MOV;*.M4A;*.MKV;*.TS",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
            };

            if (openFileDialog.ShowDialog() == false)
                return;

            sourceFolder = Path.GetDirectoryName(openFileDialog.FileName);
            Filename = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
            extension = Path.GetExtension(openFileDialog.FileName);
            player.Open(new Uri(openFileDialog.FileName));
            FileLoaded = true;
        }

        private void ConvertCommandExecute()
        {
            converter = new VideoConverter(sourceFolder, Filename, extension, OutputDifferentFormat ? $".{FormatType}" : extension, OutputDifferentFormat, ConvertToEnum());
            converter.StartedDownload += Converter_DownloadStarted;
            converter.ProgressDownload += Converter_ProgressDownload;
            converter.FinishedDownload += Converter_FinishedDownload;
            converter.ErrorDownload += Converter_ErrorDownload;
            ProgressBarViewModel = new ProgressBarViewModel();
            ProgressBarViewModel.OnCancelledHandler += (sender, args) =>
            {
                try
                {
                    converter.CancelOperation(string.Empty);
                }
                catch (Exception ex)
                {
                    ShowMessage(new MessageBoxEventArgs(ex.Message, MessageBoxEventArgs.MessageTypeEnum.Error, MessageBoxButton.OK, MessageBoxImage.Error));
                }
            };
            Task.Run(() => converter.ConvertVideo());
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

            //player.RotateNumber(RotateNumber);
            //player.Flip(FlipScale);
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

            //player.Flip(FlipScale);
            //player.RotateNumber(RotateNumber);
        }

        private ScaleRotate ConvertToEnum()
        {
            var sr = (FlipScale, RotateNumber);
            switch (sr)
            {
                case var tuple when tuple.FlipScale == 1 && tuple.RotateNumber == 0: return ScaleRotate.NoSNoR;
                case var tuple when tuple.FlipScale == 1 && tuple.RotateNumber == 90: return ScaleRotate.NoS90R;
                case var tuple when tuple.FlipScale == 1 && tuple.RotateNumber == 180: return ScaleRotate.NoS180R;
                case var tuple when tuple.FlipScale == 1 && tuple.RotateNumber == 270: return ScaleRotate.NoS270R;
                case var tuple when tuple.FlipScale == -1 && tuple.RotateNumber == 0: return ScaleRotate.SNoR;
                case var tuple when tuple.FlipScale == -1 && tuple.RotateNumber == 90: return ScaleRotate.S90R;
                case var tuple when tuple.FlipScale == -1 && tuple.RotateNumber == 180: return ScaleRotate.S180R;
                case var tuple when tuple.FlipScale == -1 && tuple.RotateNumber == 270: return ScaleRotate.S270R;
                default: throw new ArgumentOutOfRangeException(nameof(sr), sr, null);
            }
        }

        private void Converter_DownloadStarted(object sender, DownloadStartedEventArgs e) => ProgressBarViewModel.UpdateLabel(e.Label);

        private void Converter_ProgressDownload(object sender, ProgressEventArgs e)
        {
            if (e.Percentage > ProgressValue)
                ProgressBarViewModel.UpdateProgressValue(e.Percentage);
        }

        private void Converter_FinishedDownload(object sender, FinishedEventArgs e)
        {
            CleanUp();
            var message = e.Cancelled
                ? $"{Translatables.OperationCancelled} {e.Message}"
                : Translatables.VideoSuccessfullyConverted;
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
            FormatType = FormatEnum.avi;
            Filename = Translatables.NoFileSelected;
            OutputDifferentFormat = false;
            FileLoaded = false;
        }
    }
}
