using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CSVideoPlayer;
using MVVMFramework.ViewNavigator;
using MVVMFramework.Views;
using VideoEditorUi.ViewModels;
using static VideoEditorUi.Utilities.UtilityClass;

namespace VideoEditorUi.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ChapterAdderView : ViewBaseControl
    {
        public ChapterAdderView() : base()
        {
            InitializeComponent();
            playerControl.DataContext = DataContext;
            playerControl.Initialize();
        }
    }
}
