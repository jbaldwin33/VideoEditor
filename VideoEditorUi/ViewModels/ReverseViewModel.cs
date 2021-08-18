using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using MVVMFramework.ViewModels;
using MVVMFramework.ViewNavigator;
using VideoUtilities;

namespace VideoEditorUi.ViewModels
{
    public class ReverseViewModel : ViewModel
    {
        private CSVideoPlayer.VideoPlayerWPF player;
        private string filename;
        private string sourceFolder;
        private string extension;
        private bool fileLoaded;
        private decimal progressValue;
        private RelayCommand selectFileCommand;
        private RelayCommand reverseCommand;
        private ProgressBarViewModel progressBarViewModel;
        private VideoReverser reverser;

        public CSVideoPlayer.VideoPlayerWPF Player
        {
            get => player;
            set => SetProperty(ref player, value);
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

        public RelayCommand SelectFileCommand => selectFileCommand ?? (selectFileCommand = new RelayCommand(SelectFileCommandExecute, () => true));
        public RelayCommand ReverseCommand => reverseCommand ?? (reverseCommand = new RelayCommand(ReverseCommandExecute, ReverseCommandCanExecute));

        public string ReverseLabel => "Reverse";
        public string SelectFileLabel => "Click to select a file...";

        public ReverseViewModel()
        {
            
        }

        private bool ReverseCommandCanExecute() => FileLoaded;

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

        private void ReverseCommandExecute()
        {
            var messageArgs = new MessageBoxEventArgs("Reversing a video uses a lot of computer resources and can be time consuming. If you are going to reverse a video, make sure of the following:\n  1) It is best to not have other big programs running.\n  2) Video files should not exceed 1 minute.\nAre you sure you want to continue?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            ShowMessage(messageArgs);
            if (messageArgs.Result == MessageBoxResult.No)
                return;
            reverser = new VideoReverser(sourceFolder, Filename, extension);
            reverser.StartedDownload += Reverser_DownloadStarted;
            reverser.ProgressDownload += Reverser_ProgressDownload;
            reverser.FinishedDownload += Reverser_FinishedDownload;
            reverser.ErrorDownload += Reverser_ErrorDownload;
            reverser.MessageHandler += Reverser_MessageHandler;
            ProgressBarViewModel = new ProgressBarViewModel();
            ProgressBarViewModel.OnCancelledHandler += (sender, args) =>
            {
                try
                {
                    reverser.CancelOperation(string.Empty);
                }
                catch (Exception ex)
                {
                    ShowMessage(new MessageBoxEventArgs(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error));
                }
            };
            Navigator.Instance.OpenChildWindow.Execute(ProgressBarViewModel);
            Task.Run(() => reverser.ReverseVideo());
        }

        private void Reverser_DownloadStarted(object sender, DownloadStartedEventArgs e) => ProgressBarViewModel.UpdateLabel(e.Label);

        private void Reverser_ProgressDownload(object sender, ProgressEventArgs e)
        {
            if (e.Percentage > ProgressValue)
                ProgressBarViewModel.UpdateProgressValue(e.Percentage);
        }

        private void Reverser_FinishedDownload(object sender, FinishedEventArgs e)
        {
            CleanUp();
            var message = e.Cancelled
                ? $"Operation cancelled. {e.Message}"
                : "Video successfully reversed.";
            ShowMessage(new MessageBoxEventArgs(message, "Information", MessageBoxButton.OK, MessageBoxImage.Information));
        }

        private void Reverser_ErrorDownload(object sender, ProgressEventArgs e)
        {
            Navigator.Instance.CloseChildWindow.Execute(false);
            ShowMessage(new MessageBoxEventArgs($"An error has occurred. Please close and reopen the program. Check your task manager and make sure any remaining \"ffmpeg.exe\" tasks are ended.\n\n{e.Error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error));
        }

        private void Reverser_MessageHandler(object sender, MessageEventArgs e)
        {
            var args = new MessageBoxEventArgs(e.Message, "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            ShowMessage(args);
            e.Result = args.Result == MessageBoxResult.Yes;
        }

        private void CleanUp()
        {
            Navigator.Instance.CloseChildWindow.Execute(false);
            Filename = "No file selected.";
            FileLoaded = false;
        }
    }
}