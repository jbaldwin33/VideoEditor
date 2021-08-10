using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using VideoEditorNetFramework.ViewModels;
using VideoUtilities;
using static VideoUtilities.Enums.Enums;

namespace VideoEditorUi.ViewModels
{
    public class ConverterViewModel : ViewModelBase
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
        private Window window;
        private decimal progressValue;
        private string outputData;

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

        private bool ConvertCommandCanExecute() => FileLoaded;

        private void SelectFileCommandExecute()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "All Video Files|*.wmv;*.avi;*.mpg;*.mpeg;*.mp4;*.mov;*.m4a;*.mkv;*.ts;*.WMV;*.AVI;*.MPG;*.MPEG;*.MP4;*.MOV;*.M4A;*.MKV;*.TS",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
            };

            if (openFileDialog.ShowDialog() == true)
            {
                sourceFolder = Path.GetDirectoryName(openFileDialog.FileName);
                Filename = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                extension = Path.GetExtension(openFileDialog.FileName);
                FileLoaded = true;
            }
        }

        private void ConvertCommandExecute()
        {
            converter = new VideoConverter(sourceFolder, Filename, extension, $".{FormatType}");
            converter.ProgressDownload += Converter_ProgressDownload;
            converter.FinishedDownload += Converter_FinishedDownload;
            converter.ErrorDownload += Converter_ErrorDownload;
            Task.Run(() => converter.ConvertVideo());
            window = new Window
            {
                Owner = Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Width = 350,
                Height = 250
            };
            Application.Current.Dispatcher.Invoke(() => window.ShowDialog());
        }

        private void Converter_ProgressDownload(object sender, ProgressEventArgs e)
        {
            if (e.Percentage > ProgressValue)
                ProgressValue = e.Percentage;
            outputData = e.Data;
        }

        private void Converter_FinishedDownload(object sender, DownloadEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() => window.Close());
            FormatType = FormatEnum.avi;
            Filename = "No file selected.";
            FileLoaded = false;
            MessageBox.Show("Video successfully converted.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            ProgressValue = -1;
        }

        private void Converter_ErrorDownload(object sender, ProgressEventArgs e) 
            => MessageBox.Show($"An error has occurred. Please close and reopen the program. Check your task manager and make sure any remaining \"ffmpeg.exe\" tasks are ended.\n\n{e.Error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
