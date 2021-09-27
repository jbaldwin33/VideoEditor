using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MVVMFramework.Localization;
using MVVMFramework.ViewModels;
using MVVMFramework.ViewNavigator;

namespace VideoEditorUi.ViewModels
{
    public class ProgressBarViewModel : ViewModel
    {
        public event EventHandler OnCancelledHandler;
        
        private string progressLabel;
        private RelayCommand cancelCommand;
        private ObservableCollection<ProgressViewModel> progressBarCollection;

        public ObservableCollection<ProgressViewModel> ProgressBarCollection
        {
            get => progressBarCollection;
            set => SetProperty(ref progressBarCollection, value);
        }

        public string ProgressLabel
        {
            get => progressLabel;
            set => SetProperty(ref progressLabel, value);
        }

        public RelayCommand CancelCommand => cancelCommand ?? (cancelCommand = new RelayCommand(CancelCommandExecute, () => true));

        public string CancelLabel => new CancelLabelTranslatable();

        public ProgressBarViewModel(int count, List<DownloaderViewModel.UrlClass> playlists)
        {
            ProgressBarCollection = new ObservableCollection<ProgressViewModel>();
            for (var i = 0; i < count; i++)
                ProgressBarCollection.Add(new ProgressViewModel(i + 1, count, playlists.Count, playlists[i].IsPlaylist));
        }

        public void UpdateProgressValue(decimal value, int index = 0) => ProgressBarCollection[index].UpdateProgress(value);
        public void UpdateLabel(string label) => ProgressLabel = label;
        private void OnCancelled() => OnCancelledHandler?.Invoke(this, EventArgs.Empty);
        private void CancelCommandExecute()
        {
            OnCancelled();
            Navigator.Instance.CloseChildWindow.Execute(true);
        }

        public void SetFinished(int index) => ProgressBarCollection[index].VideoIndexLabel = new CompleteLabelTranslatable();
    }

    public class ProgressViewModel : ViewModel
    {
        private string videoIndexLabel;
        private decimal progressValue;
        private bool showLabel;
        private string playlistCounter;
        private bool showPlaylistCounter;

        public string VideoIndexLabel
        {
            get => videoIndexLabel;
            set => SetProperty(ref videoIndexLabel, value);
        }

        public decimal ProgressValue
        {
            get => progressValue;
            set
            {
                SetProperty(ref progressValue, value);
                if (value >= 100)
                    VideoIndexLabel = new CompleteLabelTranslatable();
            }
        }

        public bool ShowLabel
        {
            get => showLabel;
            set => SetProperty(ref showLabel, value);
        }

        public string PlaylistCounter
        {
            get => playlistCounter;
            set => SetProperty(ref playlistCounter, value);
        }

        public bool ShowPlaylistCounter
        {
            get => showPlaylistCounter;
            set => SetProperty(ref showPlaylistCounter, value);
        }


        public ProgressViewModel(int index, int total, int playlistCount, bool isPlaylist)
        {
            ShowLabel = total != 1 || isPlaylist;
            VideoIndexLabel = isPlaylist 
                ? $"{new PlaylistCounterLabelTranslatable(1, playlistCount)}:"
                : $"{new VideoCounterLabelTranslatable(index, total)}:";
        }

        public void UpdateProgress(decimal progress) => ProgressValue = progress;
        public void UpdatePlaylist(int index, int total) => VideoIndexLabel = $"{new PlaylistCounterLabelTranslatable(index, total)}:";
    }
}
