using System;
using MVVMFramework;
using MVVMFramework.ViewModels;
using MVVMFramework.ViewNavigator;

namespace VideoEditorUi.ViewModels
{
    public class ProgressBarViewModel : ViewModel
    {
        private string progressLabel;
        private decimal progressValue;
        private RelayCommand cancelCommand;

        public string ProgressLabel
        {
            get => progressLabel;
            set => SetProperty(ref progressLabel, value);
        }

        public decimal ProgressValue
        {
            get => progressValue;
            set => SetProperty(ref progressValue, value);
        }

        public RelayCommand CancelCommand => cancelCommand ?? (cancelCommand = new RelayCommand(CancelCommandExecute, () => true));

        public string CancelLabel => Translatables.CancelLabel;

        public event EventHandler OnCancelledHandler;

        private void OnCancelled() => OnCancelledHandler?.Invoke(this, EventArgs.Empty);

        public void UpdateLabel(string label) => ProgressLabel = label;
        public void UpdateProgressValue(decimal value) => ProgressValue = value;

        private void CancelCommandExecute()
        {
            OnCancelled();
            Navigator.Instance.CloseChildWindow.Execute(true);
        }
    }
}
