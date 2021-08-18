using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using MVVMFramework.ViewModels;
using MVVMFramework.ViewNavigator;
using VideoUtilities;

namespace VideoEditorUi.ViewModels
{
    public class MergerViewModel : ViewModel
    {
        private CSVideoPlayer.VideoPlayerWPF player;
        private string filename;
        private string sourceFolder;
        private string extension;
        private bool fileLoaded;
        private decimal progressValue;
        private RelayCommand selectFileCommand;
        private RelayCommand mergeCommand;
        private ProgressBarViewModel progressBarViewModel;
        private VideoMerger merger;

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
        public RelayCommand MergeCommand => mergeCommand ?? (mergeCommand = new RelayCommand(MergeCommandExecute, MergeCommandCanExecute));

        public string MergeLabel => "Merge";
        public string SelectFileLabel => "Click to select a file...";

        public MergerViewModel()
        {
            
        }

        private bool MergeCommandCanExecute() => FileLoaded;

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

        private void MergeCommandExecute()
        {
            merger = new VideoMerger(sourceFolder, Filename, extension);
            merger.StartedDownload += Converter_DownloadStarted;
            merger.ProgressDownload += Converter_ProgressDownload;
            merger.FinishedDownload += Converter_FinishedDownload;
            merger.ErrorDownload += Converter_ErrorDownload;
            ProgressBarViewModel = new ProgressBarViewModel();
            ProgressBarViewModel.OnCancelledHandler += (sender, args) =>
            {
                try
                {
                    merger.CancelOperation(string.Empty);
                }
                catch (Exception ex)
                {
                    ShowMessage(new MessageBoxEventArgs(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error));
                }
            };
            Navigator.Instance.OpenChildWindow.Execute(ProgressBarViewModel);
            Task.Run(() => merger.FormatVideo());
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
            FileLoaded = false;
        }
    }
}