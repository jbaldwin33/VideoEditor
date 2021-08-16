using MVVMFramework.ViewModels;
using MVVMFramework.ViewNavigator;

namespace VideoEditorUi.ViewModels
{
    public class MainViewModel : ViewModel
    {
        public INavigator Navigator { get; set; }

        public MainViewModel(INavigator navigator)
        {
            Navigator = navigator;
        }
    }
}
