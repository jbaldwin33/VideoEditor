using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using MVVMFramework.ViewModels;
using MVVMFramework.ViewNavigator;
using VideoUtilities;

namespace VideoEditorUi.ViewModels
{
    public class FormatterViewModel : ViewModel
    {
        private CSVideoPlayer.VideoPlayerWPF player;
        private bool changeSpeed;
        private double currentSpeed;
        private string speedLabel;
        private string filename;
        private string sourceFolder;
        private string extension;
        private bool fileLoaded;
        private bool canFormat;
        private decimal progressValue;
        private RelayCommand selectFileCommand;
        private RelayCommand formatCommand;
        private ProgressBarViewModel progressBarViewModel;
        private VideoFormatter formatter;

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
        public bool CanFormat
        {
            get => canFormat;
            set => SetProperty(ref canFormat, value);
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

        public Slider SpeedSlider { get; set; }

        public RelayCommand SelectFileCommand => selectFileCommand ?? (selectFileCommand = new RelayCommand(SelectFileCommandExecute, () => true));
        public RelayCommand FormatCommand => formatCommand ?? (formatCommand = new RelayCommand(FormatCommandExecute, FormatCommandCanExecute));

        public string FormatLabel => "Format";
        public string SelectFileLabel => "Click to select a file...";

        public FormatterViewModel()
        {
            SpeedLabel = "1x";
        }

        public void SetEvents()
        {
            SpeedSlider.ValueChanged += SpeedSlider_ValueChanged;
            SpeedSlider.Value = 1;
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

            sourceFolder = Path.GetDirectoryName(openFileDialog.FileName);
            Filename = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
            extension = Path.GetExtension(openFileDialog.FileName);
            player.Open(new Uri(openFileDialog.FileName));
            FileLoaded = true;
        }

        private void FormatCommandExecute()
        {
            formatter = new VideoFormatter(sourceFolder, Filename, extension, CurrentSpeed);
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
                    ShowMessage(new MessageBoxEventArgs(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error));
                }
            };
            Task.Run(() => formatter.FormatVideo());
            Navigator.Instance.OpenChildWindow.Execute(ProgressBarViewModel);
        }

        private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => CurrentSpeed = e.NewValue;

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
                ? $"Operation cancelled. {e.Message}"
                : "Video successfully formatted.";
            ShowMessage(new MessageBoxEventArgs(message, "Information", MessageBoxButton.OK, MessageBoxImage.Information));
        }

        private void Converter_ErrorDownload(object sender, ProgressEventArgs e)
        {
            Navigator.Instance.CloseChildWindow.Execute(false);
            ShowMessage(new MessageBoxEventArgs($"An error has occurred. Please close and reopen the program. Check your task manager and make sure any remaining \"ffmpeg.exe\" tasks are ended.\n\n{e.Error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error));
        }

        private void CleanUp()
        {
            Navigator.Instance.CloseChildWindow.Execute(false);
            Filename = "No file selected.";
            ChangeSpeed = false;
            CurrentSpeed = 1;
            Application.Current.Dispatcher.Invoke(() => SpeedSlider.Value = 1);
            FileLoaded = false;
        }
    }
}