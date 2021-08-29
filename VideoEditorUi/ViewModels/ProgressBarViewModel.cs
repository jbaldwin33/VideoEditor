using System;
using System.Collections.ObjectModel;
using MVVMFramework;
using MVVMFramework.ViewModels;
using MVVMFramework.ViewNavigator;

namespace VideoEditorUi.ViewModels
{
    //public class ProgressBarViewModel2 : ViewModel
    //{
    //    public event EventHandler OnCancelledHandler;
    //    private string progressLabel;
    //    private decimal progressValue;
    //    private RelayCommand cancelCommand;

    //    public string ProgressLabel
    //    {
    //        get => progressLabel;
    //        set => SetProperty(ref progressLabel, value);
    //    }

    //    public decimal ProgressValue
    //    {
    //        get => progressValue;
    //        set => SetProperty(ref progressValue, value);
    //    }

    //    public RelayCommand CancelCommand => cancelCommand ?? (cancelCommand = new RelayCommand(CancelCommandExecute, () => true));

    //    public string CancelLabel => Translatables.CancelLabel;

    //    public ProgressBarViewModel2()
    //    {

    //    }

    //    public void UpdateProgressValue(decimal value) => ProgressValue = value;
    //    public void UpdateLabel(string label) => ProgressLabel = label;
    //    private void OnCancelled() => OnCancelledHandler?.Invoke(this, EventArgs.Empty);
    //    private void CancelCommandExecute()
    //    {
    //        OnCancelled();
    //        Navigator.Instance.CloseChildWindow.Execute(true);
    //    }
    //}

    public class ProgressBarViewModel : ViewModel
    {
        public string CancelLabel => Translatables.CancelLabel;
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

        public void SetFinished(int index) => ProgressBarCollection[index].VideoIndexLabel = Translatables.CompleteLabel;
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
                    VideoIndexLabel = Translatables.CompleteLabel;
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
            VideoIndexLabel = $"{string.Format(Translatables.VideoCounterLabel, index, total)}:";
        }

        public void UpdateProgress(decimal progress) => ProgressValue = progress;
    }
}
