using MVVMFramework.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoEditorUi.ViewModels
{
    public class CropWindowViewModel : ViewModel
    {
        private string originalWidthHeight;
        private string newWidthHeight;
        private string position;

        public string OriginalWidthHeight
        {
            get => originalWidthHeight;
            set => SetProperty(ref originalWidthHeight, value);
        }
        
        public string NewWidthHeight
        {
            get => newWidthHeight;
            set => SetProperty(ref newWidthHeight, value);
        }

        public string Position
        {
            get => position;
            set => SetProperty(ref position, value);
        }

        public CropWindowViewModel()
        {

        }
    }
}
