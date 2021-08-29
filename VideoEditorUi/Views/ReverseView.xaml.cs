using MVVMFramework.ViewNavigator;
using MVVMFramework.Views;
using VideoEditorUi.ViewModels;

namespace VideoEditorUi.Views
{
    /// <summary>
    /// Interaction logic for ConverterView.xaml
    /// </summary>
    public partial class ReverseView : ViewBaseControl
    {
        public ReverseView() : base(Navigator.Instance.CurrentViewModel)
        {
            InitializeComponent();
            Utilities.UtilityClass.InitializePlayer(player);
            var viewModel = Navigator.Instance.CurrentViewModel as ReverseViewModel;
            viewModel.Player = player;
        }
    }
}
