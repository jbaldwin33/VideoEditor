using MVVMFramework.ViewNavigator;
using MVVMFramework.Views;
using VideoEditorUi.ViewModels;

namespace VideoEditorUi.Views
{
    /// <summary>
    /// Interaction logic for ConverterView.xaml
    /// </summary>
    public partial class FormatterView : ViewBaseControl
    {
        private readonly FormatterViewModel viewModel;

        public FormatterView() : base(Navigator.Instance.CurrentViewModel)
        {
            InitializeComponent();
            Utilities.UtilityClass.InitializePlayer(player);
            viewModel = Navigator.Instance.CurrentViewModel as FormatterViewModel;
            viewModel.Player = player;
            viewModel.SpeedSlider = speedSlider;
            viewModel.SetEvents();
        }
    }
}
