using MVVMFramework.ViewNavigator;
using MVVMFramework.Views;
using VideoEditorUi.ViewModels;

namespace VideoEditorUi.Views
{
    /// <summary>
    /// Interaction logic for ConverterView.xaml
    /// </summary>
    public partial class SpeedChangerView : ViewBaseControl
    {
        private readonly SpeedChangerViewModel viewModel;

        public SpeedChangerView() : base(Navigator.Instance.CurrentViewModel)
        {
            InitializeComponent();
            Utilities.UtilityClass.InitializePlayer(player);
            viewModel = Navigator.Instance.CurrentViewModel as SpeedChangerViewModel;
            viewModel.Player = player;
            viewModel.SpeedSlider = speedSlider;
            viewModel.SetEvents();
        }
    }
}
