using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MVVMFramework.ViewModels;
using VideoUtilities;

namespace VideoEditorUi.ViewModels
{
    public abstract class VideoViewModel<T> : ViewModel
    {
        protected BaseClass<T> videoEditor;
    }
}
