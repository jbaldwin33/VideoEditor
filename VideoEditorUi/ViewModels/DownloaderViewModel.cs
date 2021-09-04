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
using VideoEditorUi.Views;
using VideoUtilities;
using static VideoUtilities.Enums;

namespace VideoEditorUi.ViewModels
{
    public class DownloaderViewModel : ViewModel
    {
        private List<FormatTypeViewModel> formats;
        private FormatEnum formatType;
        private string outputPath;
        private RelayCommand addUrlCommand;
        private RelayCommand downloadCommand;
        private RelayCommand removeCommand;
        private RelayCommand selectOutputFolderCommand;
        private ProgressBarViewModel progressBarViewModel;
        private string textInput;
        private string selectedFile;
        private bool fileSelected;
        private bool isPlaylist;
        private ObservableCollection<string> urlCollection;
        private VideoDownloader downloader;

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

        public ObservableCollection<string> UrlCollection
        {
            get => urlCollection;
            set => SetProperty(ref urlCollection, value);
        }

        public string TextInput
        {
            get => textInput;
            set => SetProperty(ref textInput, value);
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

        public bool IsPlaylist
        {
            get => isPlaylist;
            set => SetProperty(ref isPlaylist, value);
        }


        public RelayCommand AddUrlCommand => addUrlCommand ?? (addUrlCommand = new RelayCommand(AddUrlCommandExecute, () => true));
        public RelayCommand DownloadCommand => downloadCommand ?? (downloadCommand = new RelayCommand(DownloadCommandExecute, () => UrlCollection?.Count > 0));
        public RelayCommand RemoveCommand => removeCommand ?? (removeCommand = new RelayCommand(RemoveExecute, () => FileSelected));
        public RelayCommand SelectOutputFolderCommand => selectOutputFolderCommand ?? (selectOutputFolderCommand = new RelayCommand(SelectOutputFolderCommandExecute, () => true));

        public string MergeLabel => new MergeLabelTranslatable();
        public string AddUrlLabel => new AddUrlLabelTranslatable();
        public string MoveUpLabel => new MoveUpLabelTranslatable();
        public string MoveDownLabel => new MoveDownLabelTranslatable();
        public string RemoveLabel => new RemoveLabelTranslatable();
        public string OutputFormatLabel => $"{new OutputFormatLabelTranslatable()}:";
        public string OutputFolderLabel => new OutputFolderLabelTranslatable();
        public string DownloadLabel => new DownloadLabelTranslatable();
        public string ConvertFormatLabel => new ConvertFormatLabelTranslatable();
        public string IsPlaylistLabel => new IsPlaylistTranslatable();
        public string TagText => new EnterUrlTranslatable();
        public string ConfirmLabel => new ConfirmTranslatable();

        private static readonly object _lock = new object();

        public DownloaderViewModel() { }

        public override void OnLoaded()
        {
            Initialize();
            base.OnLoaded();
        }

        public override void OnUnloaded()
        {
            UrlCollection.Clear();
            base.OnUnloaded();
        }

        private void Initialize()
        {
            Formats = FormatTypeViewModel.CreateViewModels();
            FormatType = FormatEnum.avi;
            UrlCollection = new ObservableCollection<string>();

            BindingOperations.EnableCollectionSynchronization(UrlCollection, _lock);
        }

        private void AddUrlCommandExecute()
        {
            new ChapterTitleDialogView(this, new AddUrlLabelTranslatable()) { WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = Application.Current.MainWindow }.ShowDialog();
            UrlCollection.Add(TextInput);
            TextInput = string.Empty;
        }

        private void DownloadCommandExecute()
        {
            downloader = new VideoDownloader(UrlCollection.Select(f => f).ToList(), OutputPath, $"{FormatType}", IsPlaylist);
            downloader.StartedDownload += Converter_DownloadStarted;
            downloader.ProgressDownload += Converter_ProgressDownload;
            downloader.FinishedDownload += Converter_FinishedDownload;
            downloader.ErrorDownload += Converter_ErrorDownload;
            downloader.MessageHandler += LibraryMessageHandler;
            ProgressBarViewModel = new ProgressBarViewModel(UrlCollection.Count);
            ProgressBarViewModel.OnCancelledHandler += (sender, args) =>
            {
                try
                {
                    downloader.CancelOperation(string.Empty);
                }
                catch (Exception ex)
                {
                    ShowMessage(new MessageBoxEventArgs(ex.Message, MessageBoxEventArgs.MessageTypeEnum.Error, MessageBoxButton.OK, MessageBoxImage.Error));
                }
            };
            downloader.Setup();
            Task.Run(() => downloader.DoWork(new DownloadingLabelTranslatable()));
            Navigator.Instance.OpenChildWindow.Execute(ProgressBarViewModel);
        }
        private void RemoveExecute() => UrlCollection.Remove(SelectedFile);

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
                : new VideoSuccessfullyDownloadedTranslatable();
            ShowMessage(new MessageBoxEventArgs(message, MessageBoxEventArgs.MessageTypeEnum.Information, MessageBoxButton.OK, MessageBoxImage.Information));
        }

        private void Converter_ErrorDownload(object sender, ProgressEventArgs e)
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
            UrlCollection.Clear();
            FormatType = FormatEnum.avi;
            OutputPath = null;
            Navigator.Instance.CloseChildWindow.Execute(false);
        }
    }
}