using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoEditorUi.Singletons;

namespace VideoEditorUi.ViewModels
{
    public class ProgressBarViewModel : ViewModelBase
    {
        private decimal progressValue;
        private RelayCommand cancelCommand;

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

        public void UpdateProgressValue(decimal value) => ProgressValue = value;

        private void CancelCommandExecute() => Navigator.Instance.CloseChildWindow.Execute(true);
    }
}
