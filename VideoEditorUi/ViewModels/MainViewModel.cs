using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using VideoEditorUi.Singletons;

namespace VideoEditorUi.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public INavigator Navigator { get; set; }

        public MainViewModel(INavigator navigator)
        {
            Navigator = navigator;
        }
    }
}
