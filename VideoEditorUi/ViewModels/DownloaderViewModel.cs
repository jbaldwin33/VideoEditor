using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using Microsoft.WindowsAPICodePack.Dialogs;
using MVVMFramework.Localization;
using MVVMFramework.ViewModels;
using VideoEditorUi.Views;
using VideoUtilities;
using static VideoUtilities.Enums;

namespace VideoEditorUi.ViewModels
{
    public class DownloaderViewModel : EditorViewModel
    {
        #region Fields and props

        private List<FormatTypeViewModel> formats;
        private FormatEnum formatType;
        private string outputPath;
        private RelayCommand addUrlCommand;
        private RelayCommand downloadCommand;
        private RelayCommand removeCommand;
        private RelayCommand selectOutputFolderCommand;
        private string textInput;
        private string selectedFile;
        private bool fileSelected;
        private bool isPlaylist;
        private ObservableCollection<string> urlCollection;

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

        #endregion

        #region Commands

        public RelayCommand AddUrlCommand => addUrlCommand ?? (addUrlCommand = new RelayCommand(AddUrlCommandExecute, () => true));
        public RelayCommand DownloadCommand => downloadCommand ?? (downloadCommand = new RelayCommand(DownloadCommandExecute, () => UrlCollection?.Count > 0));
        public RelayCommand RemoveCommand => removeCommand ?? (removeCommand = new RelayCommand(RemoveExecute, () => FileSelected));
        public RelayCommand SelectOutputFolderCommand => selectOutputFolderCommand ?? (selectOutputFolderCommand = new RelayCommand(SelectOutputFolderCommandExecute, () => true));

        #endregion

        #region Labels

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

        #endregion

        private static readonly object _lock = new object();

        public override void OnUnloaded()
        {
            UrlCollection.Clear();
            base.OnUnloaded();
        }

        protected override void Initialize()
        {
            Formats = FormatTypeViewModel.CreateViewModels();
            FormatType = FormatEnum.avi;
            UrlCollection = new ObservableCollection<string>();

            BindingOperations.EnableCollectionSynchronization(UrlCollection, _lock);
        }

        private void AddUrlCommandExecute()
        {
            new ChapterTitleDialogView(this) { Title = new AddUrlLabelTranslatable(), WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = Application.Current.MainWindow }.ShowDialog();
            UrlCollection.Add(TextInput);
            TextInput = string.Empty;
        }

        private void DownloadCommandExecute()
        {
            if (string.IsNullOrEmpty(OutputPath))
            {
                ShowMessage(new MessageBoxEventArgs(new SelectOutputFolderTranslatable(), MessageBoxEventArgs.MessageTypeEnum.Information, MessageBoxButton.OK, MessageBoxImage.Information));
                return;
            }
            VideoEditor = new VideoDownloader(UrlCollection.Select(f => f).ToList(), OutputPath, $"{FormatType}", IsPlaylist);
            Execute(true, StageEnum.Primary, new DownloadingLabelTranslatable(), UrlCollection.Count);
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

        protected override void FinishedDownload(object sender, FinishedEventArgs e)
        {
            base.FinishedDownload(sender, e);
            var message = e.Cancelled
                ? $"{new OperationCancelledTranslatable()} {e.Message}"
                : new VideoSuccessfullyDownloadedTranslatable();
            ShowMessage(new MessageBoxEventArgs(message, MessageBoxEventArgs.MessageTypeEnum.Information, MessageBoxButton.OK, MessageBoxImage.Information));
        }

        protected override void CleanUp()
        {
            UrlCollection.Clear();
            FormatType = FormatEnum.avi;
            OutputPath = null;
            base.CleanUp();
        }
    }
}