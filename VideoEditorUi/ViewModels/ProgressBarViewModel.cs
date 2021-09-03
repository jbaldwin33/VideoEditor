using System;
using System.Collections.ObjectModel;
using MVVMFramework;
using MVVMFramework.Localization;
using MVVMFramework.ViewModels;
using MVVMFramework.ViewNavigator;

namespace VideoEditorUi.ViewModels
{
    public class ProgressBarViewModel : ViewModel
    {
        public string CancelLabel => new CancelLabelTranslatable();
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

        public ProgressBarViewModel(int count = 1)
        {
            ProgressBarCollection = new ObservableCollection<ProgressViewModel>();
            for (int i = 0; i < count; i++)
                ProgressBarCollection.Add(new ProgressViewModel(i + 1, count));
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


        public ProgressViewModel(int index, int total)
        {
            ShowLabel = total != 1;
            VideoIndexLabel = $"{new VideoCounterLabelTranslatable(index, total)}:";
        }

        public void UpdateProgress(decimal progress) => ProgressValue = progress;
    }
}
