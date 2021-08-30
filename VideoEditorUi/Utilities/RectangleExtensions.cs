using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shapes;

namespace VideoEditorUi.Utilities
{
    public static class ExecutesCommandOnLeftClickBehavior
    {
        public static ICommand GetCommand(DependencyObject obj) => (ICommand)obj.GetValue(CommandProperty);

        public static void SetCommand(DependencyObject obj, ICommand value) => obj.SetValue(CommandProperty, value);

        public static readonly DependencyProperty CommandProperty = DependencyProperty.RegisterAttached(
        "Command",
        typeof(ICommand),
        typeof(ExecutesCommandOnLeftClickBehavior),
        new PropertyMetadata(null, OnCommandPropertyChanged));

        private static void OnCommandPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is UIElement element))
                return;

            element.MouseLeftButtonDown -= OnMouseLeftButtonDown;
            element.MouseLeftButtonDown += OnMouseLeftButtonDown;
        }

        private static void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //UIElement element = sender as UIElement;
            if (!(sender is UIElement element))
                return;

            var command = GetCommand(element);
            if (command == null)
                return;

            if (command.CanExecute(null))
                command.Execute((sender as Rectangle).DataContext);
        }
    }
}
