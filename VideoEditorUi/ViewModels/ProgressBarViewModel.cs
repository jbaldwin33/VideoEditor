using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using VideoEditorUi.Singletons;

namespace VideoEditorUi.ViewModels
{
    public class ProgressBarViewModel : ViewModelBase
    {
        private string progressLabel;
        private decimal progressValue;
        private RelayCommand cancelCommand;

        public string ProgressLabel
        {
            get => progressLabel;
            set => Set(ref progressLabel, value);
        }

        public decimal ProgressValue
        {
            get => progressValue;
            set => Set(ref progressValue, value);
        }

        public RelayCommand CancelCommand => cancelCommand ?? (cancelCommand = new RelayCommand(CancelCommandExecute, () => true));

        public string CancelLabel => "Cancel";

        public ProgressBarViewModel()
        {
            
        }

        public void UpdateLabel(string label) => ProgressLabel = label;
        public void UpdateProgressValue(decimal value) => ProgressValue = value;

        private void CancelCommandExecute() => Navigator.Instance.CloseChildWindow.Execute(true);
    }
}
