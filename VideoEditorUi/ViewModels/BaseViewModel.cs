using System;
using GalaSoft.MvvmLight;

namespace VideoEditorUi.ViewModels
{
    public class BaseViewModel : ViewModelBase
    {
        public virtual void CancelOperation() => throw new NotImplementedException();
    }
}