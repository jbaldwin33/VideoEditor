using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using MVVMFramework.ViewNavigator;

namespace VideoEditorUi.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(Type[] viewModelTypes)
        {
            //if (viewModelTypes == null || viewModelTypes.Length == 0)
            //    throw new ArgumentNullException(nameof(viewModelTypes));

            //InitializeComponent();
            //foreach (var type in viewModelTypes)
            //{
            //    var button = new Button { CommandParameter = type };
            //    var binding = new Binding("Title") { Source = Activator.CreateInstance(type) };
            //    var binding2 = new Binding(nameof(Navigator.Instance.UpdateCurrentViewModelCommand));

            //    button.SetBinding(ContentProperty, binding);
            //    button.SetBinding(ButtonBase.CommandProperty, binding2);

            //    navigationBar.GetStackPanel().Children.Add(button);
            //}
        }
    }
}
