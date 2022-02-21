using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MVVMFrameworkNet472.Localization;
using MVVMFrameworkNet472.ViewModels;
using MVVMFrameworkNet472.ViewNavigator;
using VideoEditorUi.Utilities;
using VideoUtilities;

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

        public ProgressBarViewModel(int count, List<UrlClass> urls)
        {
            ProgressBarCollection = new ObservableCollection<ProgressViewModel>();
            for (var i = 0; i < count; i++)
            {
                if (urls == null)
                    ProgressBarCollection.Add(new ProgressViewModel(i + 1, count));
                else
                {
                    var numberOfPlaylists = urls.Count(p => p.IsPlaylist);
                    var index = urls[i].IsPlaylist ? 1 : i + 1 - numberOfPlaylists;
                    var total = urls[i].IsPlaylist ? count : count - numberOfPlaylists;
                    ProgressBarCollection.Add(new ProgressViewModel(index, total, urls[i].IsPlaylist));
                }
            }
        }

        public void UpdateProgressValue(decimal value, int index = 0) => ProgressBarCollection[index].UpdateProgress(value);
        public void UpdateLabel(string label) => ProgressLabel = label;
        private void OnCancelled() => OnCancelledHandler?.Invoke(this, EventArgs.Empty);
        private void CancelCommandExecute()
        {
            OnCancelled();
            UtilityClass.Instance.CloseChildWindow(true);
        }

        public void SetFinished(int index) => ProgressBarCollection[index].VideoIndexLabel = new CompleteLabelTranslatable();
    }

    public class ProgressViewModel : ViewModel
    {
        private string videoIndexLabel;
        private decimal progressValue;
        private string progressValueString;
        private bool showLabel;

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
                ProgressValueString = value > 100 ? "100.0%" : $"{value:0.0}%";
                if (value >= 100)
                    VideoIndexLabel = new CompleteLabelTranslatable();
            }
        }

        public string ProgressValueString
        {
            get => progressValueString;
            set => SetProperty(ref progressValueString, value);
        }


        public bool ShowLabel
        {
            get => showLabel;
            set => SetProperty(ref showLabel, value);
        }

        //for video
        public ProgressViewModel(int index, int total)
        {
            ProgressValueString = "0.0%";
            ShowLabel = total != 1;
            VideoIndexLabel = $"{new VideoCounterLabelTranslatable(index, total)}:";
        }

        //for video and playlist
        public ProgressViewModel(int index, int total, bool isPlaylist)
        {
            ProgressValueString = "0.0%";
            ShowLabel = total != 1 || isPlaylist;
            VideoIndexLabel = isPlaylist
                ? $"{new PlaylistCounterLabelTranslatable(1, 1)}:"
                : $"{new VideoCounterLabelTranslatable(index, total)}:";
        }

        public void UpdateProgress(decimal progress) => ProgressValue = progress;
        public void UpdatePlaylist(int index, int total) => VideoIndexLabel = $"{new PlaylistCounterLabelTranslatable(index, total)}:";
    }
}
