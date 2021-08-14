using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using VideoEditorNetFramework.ViewModels;
using VideoEditorUi.Singletons;
using VideoUtilities;
using static VideoUtilities.Enums.Enums;

namespace VideoEditorUi.ViewModels
{
    public class ConverterViewModel : BaseViewModel
    {
        private ObservableCollection<FormatTypeViewModel> formats;
        private FormatEnum formatType;
        private string filename;
        private RelayCommand selectFileCommand;
        private RelayCommand convertCommand;
        private string sourceFolder;
        private string extension;
        private bool fileLoaded;
        private VideoConverter converter;
        private decimal progressValue;

        public ObservableCollection<FormatTypeViewModel> Formats
        {
            get => formats;
            set => Set(ref formats, value);
        }

        public FormatEnum FormatType
        {
            get => formatType;
            set => Set(ref formatType, value);
        }

        public string Filename
        {
            get => filename;
            set => Set(ref filename, value);
        }

        public bool FileLoaded
        {
            get => fileLoaded;
            set
            {
                Set(ref fileLoaded, value);
                Application.Current.Dispatcher.Invoke(() => ConvertCommand.RaiseCanExecuteChanged());
            }
        }

        public decimal ProgressValue
        {
            get => progressValue;
            set => Set(ref progressValue, value);
        }

        public RelayCommand SelectFileCommand => selectFileCommand ?? (selectFileCommand = new RelayCommand(SelectFileCommandExecute, () => true));
        public RelayCommand ConvertCommand => convertCommand ?? (convertCommand = new RelayCommand(ConvertCommandExecute, ConvertCommandCanExecute));

        public string SelectFileLabel => "Click to select a file...";
        public string ConvertLabel => "Convert";

        public ConverterViewModel()
        {
            Formats = FormatTypeViewModel.CreateViewModels();
            Filename = "No file selected.";
        }

        public override void CancelOperation() => converter.CancelOperation();

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
            FileLoaded = true;
        }

        private void ConvertCommandExecute()
        {
            converter = new VideoConverter(sourceFolder, Filename, extension, $".{FormatType}");
            converter.StartedDownload += Converter_DownloadStarted;
            converter.ProgressDownload += Converter_ProgressDownload;
            converter.FinishedDownload += Converter_FinishedDownload;
            converter.ErrorDownload += Converter_ErrorDownload;
            Navigator.Instance.OpenChildWindow.Execute(null);
            Task.Run(() => converter.ConvertVideo());
        }

        private void Converter_DownloadStarted(object sender, DownloadStartedEventArgs e)
        {
            if (Navigator.Instance.ChildViewModel is ProgressBarViewModel vm)
                vm.UpdateLabel(e.Label);
        }

        private void Converter_ProgressDownload(object sender, ProgressEventArgs e)
        {
            if (e.Percentage > ProgressValue && Navigator.Instance.ChildViewModel is ProgressBarViewModel vm)
                vm.UpdateProgressValue(e.Percentage);
        }

        private void Converter_FinishedDownload(object sender, FinishedEventArgs e)
        {
            CleanUp();
            var message = e.Cancelled
                ? "Operation cancelled."
                : "Video successfully converted.";
            MessageBox.Show(message, "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Converter_ErrorDownload(object sender, ProgressEventArgs e)
        {
            Navigator.Instance.CloseChildWindow.Execute(false);
            MessageBox.Show($"An error has occurred. Please close and reopen the program. Check your task manager and make sure any remaining \"ffmpeg.exe\" tasks are ended.\n\n{e.Error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void CleanUp()
        {
            Navigator.Instance.CloseChildWindow.Execute(false);
            FormatType = FormatEnum.avi;
            Filename = "No file selected.";
            FileLoaded = false;
        }
    }
}
