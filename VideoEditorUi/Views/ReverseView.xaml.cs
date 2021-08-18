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
        private readonly ReverseViewModel viewModel;

        public ReverseView() : base(Navigator.Instance.CurrentViewModel)
        {
            InitializeComponent();
            Utilities.UtilityClass.InitializePlayer(player);
            viewModel = Navigator.Instance.CurrentViewModel as ReverseViewModel;
            viewModel.Player = player;
        }
    }
}
