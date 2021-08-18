using MVVMFramework.ViewNavigator;
using MVVMFramework.Views;
using VideoEditorUi.ViewModels;

namespace VideoEditorUi.Views
{
    /// <summary>
    /// Interaction logic for ConverterView.xaml
    /// </summary>
    public partial class ConverterView : ViewBaseControl
    {
        private readonly ConverterViewModel viewModel;

        public ConverterView() : base(Navigator.Instance.CurrentViewModel)
        {
            InitializeComponent();
            Utilities.UtilityClass.InitializePlayer(player);
            viewModel = Navigator.Instance.CurrentViewModel as ConverterViewModel;
            viewModel.Player = player;
            viewModel.VideoStackPanel = stackPanel;
        }
    }
}
