using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using MVVMFramework;
using MVVMFramework.Localization;
using MVVMFramework.ViewModels;
using MVVMFramework.ViewNavigator;
using VideoEditorUi.Utilities;
using VideoUtilities;
using static VideoUtilities.Enums;

namespace VideoEditorUi.ViewModels
{
    public class DownloaderViewModel : ViewModel
    {
        private List<FormatTypeViewModel> formats;
        private FormatEnum formatType;
        private string outputPath;
        private RelayCommand selectFileCommand;
        private RelayCommand reduceSizeCommand;
        private RelayCommand convertCommand;
        private RelayCommand removeCommand;
        private RelayCommand selectOutputFolderCommand;
        private ProgressBarViewModel progressBarViewModel;
        private VideoSizeReducer sizeReducer;
        private VideoConverter converter;
        private string selectedFile;
        private bool fileSelected;
        private ObservableCollection<string> fileCollection;
        private bool converterSelected;

        public List<FormatTypeViewModel> Formats
        {
            get => formats;
            set => SetProperty(ref formats, value);
        }

        public FormatEnum FormatType
        {
            get => formatType;
            set => SetProperty(ref formatType, value);
        }

        public string OutputPath
        {
            get => outputPath;
            set => SetProperty(ref outputPath, value);
        }

        public ProgressBarViewModel ProgressBarViewModel
        {
            get => progressBarViewModel;
            set => SetProperty(ref progressBarViewModel, value);
        }

        public ObservableCollection<string> FileCollection
        {
            get => fileCollection;
            set => SetProperty(ref fileCollection, value);
        }

        public string SelectedFile
        {
            get => selectedFile;
            set
            {
                SetProperty(ref selectedFile, value);
                FileSelected = !string.IsNullOrEmpty(value);
            }
        }

        public bool FileSelected
        {
            get => fileSelected;
            set => SetProperty(ref fileSelected, value);
        }

        public bool ConverterSelected
        {
            get => converterSelected;
            set => SetProperty(ref converterSelected, value);
        }



        public RelayCommand SelectFileCommand => selectFileCommand ?? (selectFileCommand = new RelayCommand(SelectFileCommandExecute, () => true));
        public RelayCommand ReduceSizeCommand => reduceSizeCommand ?? (reduceSizeCommand = new RelayCommand(ReduceSizeCommandExecute, () => FileCollection?.Count > 0));
        public RelayCommand ConvertCommand => convertCommand ?? (convertCommand = new RelayCommand(ConvertCommandExecute, () => FileCollection?.Count > 0));
        public RelayCommand RemoveCommand => removeCommand ?? (removeCommand = new RelayCommand(RemoveExecute, () => FileSelected));
        public RelayCommand SelectOutputFolderCommand => selectOutputFolderCommand ?? (selectOutputFolderCommand = new RelayCommand(SelectOutputFolderCommandExecute, () => true));

        public string MergeLabel => new MergeLabelTranslatable();
        public string SelectFileLabel => new SelectFileLabelTranslatable();
        public string MoveUpLabel => new MoveUpLabelTranslatable();
        public string MoveDownLabel => new MoveDownLabelTranslatable();
        public string RemoveLabel => new RemoveLabelTranslatable();
        public string OutputFormatLabel => $"{new OutputFormatLabelTranslatable()}:";
        public string OutputFolderLabel => new OutputFolderLabelTranslatable();
        public string ReduceVideoSizeLabel => new ReduceVideoSizeLabelTranslatable();
        public string ConvertLabel => new ConvertLabelTranslatable();
        public string ConvertFormatLabel => new ConvertFormatLabelTranslatable();

        private static readonly object _lock = new object();

        public DownloaderViewModel() { }

        public override void OnLoaded()
        {
            Initialize();
            base.OnLoaded();
        }

        public override void OnUnloaded()
        {
            FileCollection.Clear();
            base.OnUnloaded();
        }

        private void Initialize()
        {
            ConverterSelected = true;
            Formats = FormatTypeViewModel.CreateViewModels();
            FormatType = FormatEnum.avi;
            FileCollection = new ObservableCollection<string>();

            BindingOperations.EnableCollectionSynchronization(FileCollection, _lock);
        }

        private void SelectFileCommandExecute()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "All Video Files|*.wmv;*.avi;*.mpg;*.mpeg;*.mp4;*.mov;*.m4a;*.mkv;*.ts;*.WMV;*.AVI;*.MPG;*.MPEG;*.MP4;*.MOV;*.M4A;*.MKV;*.TS",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == false)
                return;

            openFileDialog.FileNames.ToList().ForEach(FileCollection.Add);
        }

        private void ReduceSizeCommandExecute()
        {
            sizeReducer = new VideoSizeReducer(FileCollection.Select(f => (Path.GetDirectoryName(f), Path.GetFileNameWithoutExtension(f), Path.GetExtension(f))), OutputPath);
            sizeReducer.StartedDownload += SizeReducer_DownloadStarted;
            sizeReducer.ProgressDownload += SizeReducer_ProgressDownload;
            sizeReducer.FinishedDownload += SizeReducer_FinishedDownload;
            sizeReducer.ErrorDownload += SizeReducer_ErrorDownload;
            sizeReducer.MessageHandler += LibraryMessageHandler;
            ProgressBarViewModel = new ProgressBarViewModel(FileCollection.Count);
            ProgressBarViewModel.OnCancelledHandler += (sender, args) =>
            {
                try
                {
                    sizeReducer.CancelOperation(string.Empty);
                }
                catch (Exception ex)
                {
                    ShowMessage(new MessageBoxEventArgs(ex.Message, MessageBoxEventArgs.MessageTypeEnum.Error, MessageBoxButton.OK, MessageBoxImage.Error));
                }
            };
            sizeReducer.Setup();
            Task.Run(() => sizeReducer.DoWork(new ReducingSizeLabelTranslatable()));
            Navigator.Instance.OpenChildWindow.Execute(ProgressBarViewModel);
        }

        private void ConvertCommandExecute()
        {
            converter = new VideoConverter(FileCollection.Select(f => (Path.GetDirectoryName(f), Path.GetFileNameWithoutExtension(f), Path.GetExtension(f))), $".{FormatType}");
            converter.StartedDownload += Converter_DownloadStarted;
            converter.ProgressDownload += Converter_ProgressDownload;
            converter.FinishedDownload += Converter_FinishedDownload;
            converter.ErrorDownload += Converter_ErrorDownload;
            converter.MessageHandler += LibraryMessageHandler;
            ProgressBarViewModel = new ProgressBarViewModel(FileCollection.Count);
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
            converter.Setup();
            Task.Run(() => converter.DoWork(new ConvertingLabelTranslatable()));
            Navigator.Instance.OpenChildWindow.Execute(ProgressBarViewModel);
        }
        private void RemoveExecute() => FileCollection.Remove(SelectedFile);

        private void SelectOutputFolderCommandExecute()
        {
            var openFolderDialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
            };

            if (openFolderDialog.ShowDialog() == CommonFileDialogResult.Cancel)
                return;

            OutputPath = openFolderDialog.FileName;
        }
        private void Converter_DownloadStarted(object sender, DownloadStartedEventArgs e) => ProgressBarViewModel.UpdateLabel(e.Label);

        private void Converter_ProgressDownload(object sender, ProgressEventArgs e)
        {
            if (e.Percentage > ProgressBarViewModel.ProgressBarCollection[e.ProcessIndex].ProgressValue)
                ProgressBarViewModel.UpdateProgressValue(e.Percentage, e.ProcessIndex);
        }

        private void Converter_FinishedDownload(object sender, FinishedEventArgs e)
        {
            CleanUp();
            var message = e.Cancelled
                ? $"{new OperationCancelledTranslatable()} {e.Message}"
                : new VideoSuccessfullyConvertedTranslatable();
            ShowMessage(new MessageBoxEventArgs(message, MessageBoxEventArgs.MessageTypeEnum.Information, MessageBoxButton.OK, MessageBoxImage.Information));
        }

        private void Converter_ErrorDownload(object sender, ProgressEventArgs e)
        {
            Navigator.Instance.CloseChildWindow.Execute(false);
            ShowMessage(new MessageBoxEventArgs($"{new ErrorOccurredTranslatable()}\n\n{e.Error}", MessageBoxEventArgs.MessageTypeEnum.Error, MessageBoxButton.OK, MessageBoxImage.Error));
        }

        private void SizeReducer_DownloadStarted(object sender, DownloadStartedEventArgs e) => ProgressBarViewModel.UpdateLabel(e.Label);

        private void SizeReducer_ProgressDownload(object sender, ProgressEventArgs e)
        {
            if (e.Percentage > ProgressBarViewModel.ProgressBarCollection[e.ProcessIndex].ProgressValue)
                ProgressBarViewModel.UpdateProgressValue(e.Percentage, e.ProcessIndex);
        }

        private void SizeReducer_FinishedDownload(object sender, FinishedEventArgs e)
        {
            ProgressBarViewModel.SetFinished(e.ProcessIndex);
            CleanUp();
            var message = e.Cancelled
                ? $"{new OperationCancelledTranslatable()} {e.Message}"
                : new SizeSuccessfullyReducedTranslatable();
            ShowMessage(new MessageBoxEventArgs(message, MessageBoxEventArgs.MessageTypeEnum.Information, MessageBoxButton.OK, MessageBoxImage.Information));
        }

        private void SizeReducer_ErrorDownload(object sender, ProgressEventArgs e)
        {
            Navigator.Instance.CloseChildWindow.Execute(false);
            ShowMessage(new MessageBoxEventArgs($"{new ErrorOccurredTranslatable()}\n\n{e.Error}", MessageBoxEventArgs.MessageTypeEnum.Error, MessageBoxButton.OK, MessageBoxImage.Error));
        }

        private void LibraryMessageHandler(object sender, MessageEventArgs e)
        {
            var args = new MessageBoxEventArgs(e.Message, MessageBoxEventArgs.MessageTypeEnum.Question, MessageBoxButton.YesNo, MessageBoxImage.Question);
            ShowMessage(args);
            e.Result = args.Result == MessageBoxResult.Yes;
        }

        private void CleanUp()
        {
            FileCollection.Clear();
            FormatType = FormatEnum.avi;
            OutputPath = null;
            Navigator.Instance.CloseChildWindow.Execute(false);
        }
    }
}