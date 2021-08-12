using VideoEditorUi.Singletons;

namespace VideoEditorUi.ViewModels
{
    public class PopupWindowViewModel
    {
        public INavigator Navigator { get; set; }
        public PopupWindowViewModel(INavigator navigator)
        {
            Navigator = navigator;
        }
    }
}