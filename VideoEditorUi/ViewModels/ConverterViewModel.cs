//using Microsoft.Win32;
//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using System.Windows;
//using MVVMFramework;
//using MVVMFramework.ViewNavigator;
//using MVVMFramework.ViewModels;
//using VideoEditorUi.Utilities;
//using VideoUtilities;
//using static VideoUtilities.Enums.Enums;

//namespace VideoEditorUi.ViewModels
//{
//    public class ConverterViewModel : ViewModel
//    {
//        private List<FormatTypeViewModel> formats;
//        private FormatEnum formatType;
//        private List<string> inputPath;
//        private RelayCommand selectFileCommand;
//        private RelayCommand convertCommand;
//        private bool fileLoaded;
//        private bool outputDifferentFormat;
//        private VideoConverter converter;
//        private ProgressBarViewModel progressBarViewModel;
//        private CSVideoPlayer.VideoPlayerWPF player;
        

//        public List<FormatTypeViewModel> Formats
//        {
//            get => formats;
//            set => SetProperty(ref formats, value);
//        }

//        public FormatEnum FormatType
//        {
//            get => formatType;
//            set => SetProperty(ref formatType, value);
//        }

//        public List<string> InputPath
//        {
//            get => inputPath;
//            set => SetProperty(ref inputPath, value);
//        }

//        public bool FileLoaded
//        {
//            get => fileLoaded;
//            set => SetProperty(ref fileLoaded, value);
//        }

//        public bool OutputDifferentFormat
//        {
//            get => outputDifferentFormat;
//            set => SetProperty(ref outputDifferentFormat, value);
//        }

//        public ProgressBarViewModel ProgressBarViewModel
//        {
//            get => progressBarViewModel;
//            set => SetProperty(ref progressBarViewModel, value);
//        }

//        public CSVideoPlayer.VideoPlayerWPF Player
//        {
//            get => player;
//            set => SetProperty(ref player, value);
//        }

//        public RelayCommand SelectFileCommand => selectFileCommand ?? (selectFileCommand = new RelayCommand(SelectFileCommandExecute, () => true));
//        public RelayCommand ConvertCommand => convertCommand ?? (convertCommand = new RelayCommand(ConvertCommandExecute, ConvertCommandCanExecute));

//        public string SelectFileLabel => Translatables.SelectFileLabel;
//        public string ConvertLabel => Translatables.ConvertLabel;
//        public string FlipLabel => Translatables.FlipLabel;
//        public string RotateLabel => Translatables.RotateLabel;
//        public string OutputFormatLabel => $"{Translatables.OutputFormatLabel}:";
//        public string NoFileLabel => Translatables.NoFileSelected;
//        public string ReduceSizeLabel => Translatables.ReduceSizeLabel;

//        public ConverterViewModel() { }

//        public override void OnLoaded()
//        {
//            Formats = FormatTypeViewModel.CreateViewModels();
//            FormatType = FormatEnum.avi;
//            InputPath = new List<string>();
//            base.OnLoaded();
//        }

//        public override void OnUnloaded()
//        {
//            UtilityClass.ClosePlayer(player);
//            FileLoaded = false;
//            base.OnUnloaded();
//        }

//        private bool ConvertCommandCanExecute() => FileLoaded;

//        private void SelectFileCommandExecute()
//        {
//            var openFileDialog = new OpenFileDialog
//            {
//                Filter = "All Video Files|*.wmv;*.avi;*.mpg;*.mpeg;*.mp4;*.mov;*.m4a;*.mkv;*.ts;*.WMV;*.AVI;*.MPG;*.MPEG;*.MP4;*.MOV;*.M4A;*.MKV;*.TS",
//                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
//            };

//            if (openFileDialog.ShowDialog() == false)
//                return;

//            InputPath.AddRange(openFileDialog.FileNames);
//            player.Open(new Uri(openFileDialog.FileName));
//            FileLoaded = true;
//        }

//        private void ConvertCommandExecute()
//        {
//            //converter = new VideoConverter(InputPath, $".{FormatType}");
//            converter.StartedDownload += Converter_DownloadStarted;
//            converter.ProgressDownload += Converter_ProgressDownload;
//            converter.FinishedDownload += Converter_FinishedDownload;
//            converter.ErrorDownload += Converter_ErrorDownload;
//            ProgressBarViewModel = new ProgressBarViewModel();
//            ProgressBarViewModel.OnCancelledHandler += (sender, args) =>
//            {
//                try
//                {
//                    converter.CancelOperation(string.Empty);
//                }
//                catch (Exception ex)
//                {
//                    ShowMessage(new MessageBoxEventArgs(ex.Message, MessageBoxEventArgs.MessageTypeEnum.Error, MessageBoxButton.OK, MessageBoxImage.Error));
//                }
//            };
//            Task.Run(() => converter.ConvertVideo());
//            Navigator.Instance.OpenChildWindow.Execute(ProgressBarViewModel);
//        }
        
        

//        private void Converter_DownloadStarted(object sender, DownloadStartedEventArgs e) => ProgressBarViewModel.UpdateLabel(e.Label);

//        private void Converter_ProgressDownload(object sender, ProgressEventArgs e)
//        {
//            if (e.Percentage > ProgressBarViewModel.ProgressBarCollection[e.ProcessIndex].ProgressValue)
//                ProgressBarViewModel.UpdateProgressValue(e.Percentage);
//        }

//        private void Converter_FinishedDownload(object sender, FinishedEventArgs e)
//        {
//            CleanUp();
//            var message = e.Cancelled
//                ? $"{Translatables.OperationCancelled} {e.Message}"
//                : Translatables.VideoSuccessfullyConverted;
//            ShowMessage(new MessageBoxEventArgs(message, MessageBoxEventArgs.MessageTypeEnum.Information, MessageBoxButton.OK, MessageBoxImage.Information));
//        }

//        private void Converter_ErrorDownload(object sender, ProgressEventArgs e)
//        {
//            Navigator.Instance.CloseChildWindow.Execute(false);
//            ShowMessage(new MessageBoxEventArgs($"{Translatables.ErrorOccurred}\n\n{e.Error}", MessageBoxEventArgs.MessageTypeEnum.Error, MessageBoxButton.OK, MessageBoxImage.Error));
//        }

//        private void CleanUp()
//        {
//            Navigator.Instance.CloseChildWindow.Execute(false);
//            FormatType = FormatEnum.avi;
//            InputPath.Clear();
//            OutputDifferentFormat = false;
//            FileLoaded = false;
//        }
//    }
//}
