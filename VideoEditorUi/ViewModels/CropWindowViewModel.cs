using MVVMFramework.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoUtilities;

namespace VideoEditorUi.ViewModels
{
    public class CropWindowViewModel : ViewModel
    {
        #region Fields and props

        private string oldSize;
        private string newSize;
        private string position;
        private CropClass cropClass;
        private RelayCommand setCropCommand;

        public Action SetCrop;

        public string OldSize
        {
            get => oldSize;
            set => SetProperty(ref oldSize, value);
        }

        public string NewSize
        {
            get => newSize;
            set => SetProperty(ref newSize, value);
        }

        public string Position
        {
            get => position;
            set => SetProperty(ref position, value);
        }

        public CropClass CropClass
        {
            get => cropClass;
            set => SetProperty(ref cropClass, value);
        }

        #endregion

        public RelayCommand SetCropCommand => setCropCommand ?? (setCropCommand = new RelayCommand(SetCropCommandExecute, () => true));

        public string SetCropLabel => "Set Crop"; //new SetCropLabelTranslatable();

        #region Disable binding errors
#if DEBUG
        public RelayCommand SeekBackCommand { get; set; }
        public RelayCommand SeekForwardCommand { get; set; }
        public RelayCommand PlayCommand { get; set; }
#endif
        #endregion

        private void SetCropCommandExecute() => SetCrop?.Invoke();

        public CropWindowViewModel()
        {
            CropClass = new CropClass();
        }
    }
}
