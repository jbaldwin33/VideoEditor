using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using MVVMFramework.Localization;
using MVVMFramework.ViewModels;
using VideoEditorUi.Views;
using VideoUtilities;

namespace VideoEditorUi.ViewModels
{
    public class DownloaderViewModel : EditorViewModel
    {
        #region Fields and props

        private string outputPath;
        private RelayCommand addUrlCommand;
        private RelayCommand downloadCommand;
        private RelayCommand removeCommand;
        private RelayCommand selectOutputFolderCommand;
        private string textInput;
        private string selectedFile;
        private bool fileSelected;
        private ObservableCollection<UrlClass> urlCollection;
        private bool extractAudio;
        private bool addedVisible;

        public string OutputPath
        {
            get => outputPath;
            set => SetProperty(ref outputPath, value);
        }

        public ObservableCollection<UrlClass> UrlCollection
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

        public bool ExtractAudio
        {
            get => extractAudio;
            set => SetProperty(ref extractAudio, value);
        }

        public bool AddedVisible
        {
            get => addedVisible;
            set => SetProperty(ref addedVisible, value);
        }


        public Action AddUrl;

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
        public string ExtractAudioLabel => new DownloadAudioOnlyTranslatable();
        public string TagText => new EnterUrlTranslatable();
        public string AddLabel => new AddTranslatable();
        public string DoneLabel => new DoneTranslatable();
        public string UrlAddedLabel => new UrlAddedTranslatable();
        public string UrlCommentLabel => new AddUrlsForVideoPlaylistTranslatable();

        #endregion

        private static readonly object _lock = new object();

        public override void OnUnloaded()
        {
            UrlCollection.Clear();
            base.OnUnloaded();
        }

        public override void Initialize()
        {
            AddUrl = () =>
            {
                UrlCollection.Add(new UrlClass(TextInput, IsPlaylist(TextInput)));
                TextInput = string.Empty;
                Task.Run(ToggleLabel);
            };
            UrlCollection = new ObservableCollection<UrlClass>();
            BindingOperations.EnableCollectionSynchronization(UrlCollection, _lock);
        }

        private void AddUrlCommandExecute()
        {
            var view = new UrlDialogView { DataContext = this };
            view.Initialize();
            view.ShowDialog();
        }

        private void DownloadCommandExecute()
        {
            if (string.IsNullOrEmpty(OutputPath))
            {
                ShowMessage(new MessageBoxEventArgs(new SelectOutputFolderTranslatable(), MessageBoxEventArgs.MessageTypeEnum.Information, MessageBoxButton.OK, MessageBoxImage.Information));
                return;
            }
            var urls = UrlCollection.OrderByDescending(u => u.IsPlaylist);
            VideoEditor = new VideoDownloader(urls.Select(u => (u.Url, u.IsPlaylist)), ExtractAudio, OutputPath);
            Setup(true, UrlCollection.Count, urls.ToList());
            Execute(StageEnum.Primary, new DownloadingLabelTranslatable());
        }

        private void RemoveExecute() => UrlCollection.Remove(UrlCollection.First(u => u.Url == SelectedFile));

        private void SelectOutputFolderCommandExecute()
        {
            var openFolderDialog = new FolderBrowserDialog
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
            };

            if (openFolderDialog.ShowDialog() == DialogResult.Cancel)
                return;

            OutputPath = openFolderDialog.SelectedPath;
        }

        private bool IsPlaylist(string url) => url.Contains("playlist");

        private void ToggleLabel()
        {
            AddedVisible = true;
            Thread.Sleep(2000);
            AddedVisible = false;
        }

        protected override void FinishedDownload(object sender, FinishedEventArgs e)
        {
            base.FinishedDownload(sender, e);
            var message = e.Cancelled
                ? $"{new OperationCancelledTranslatable()}\n{e.Message}"
                : new VideoSuccessfullyDownloadedTranslatable();
            ShowMessage(new MessageBoxEventArgs(message, MessageBoxEventArgs.MessageTypeEnum.Information, MessageBoxButton.OK, MessageBoxImage.Information));
        }

        public override void CleanUp(bool isError)
        {
            if (!isError)
            {
                UrlCollection.Clear();
                ExtractAudio = false;
                OutputPath = null;
            }
            base.CleanUp(isError);
        }

        public class UrlClass
        {
            public string Url { get; set; }
            public bool IsPlaylist { get; set; }

            public UrlClass(string url, bool isPlaylist)
            {
                Url = isPlaylist || !url.Contains("list") ? url : url.Substring(0, url.IndexOf("list") - 1);
                IsPlaylist = isPlaylist;
            }
        }
    }
}