using System;
using MVVMFramework.ViewModels;

namespace VideoEditorUi.ViewModels
{
    public abstract class VideoViewModel : ViewModel
    {
        public virtual void CancelOperation() => throw new NotImplementedException();
    }
}