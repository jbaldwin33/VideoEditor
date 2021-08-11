using GalaSoft.MvvmLight;
using System;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows;
using VideoEditorUi.ViewModels;
using VideoEditorUi.Views;

namespace VideoEditorUi.Singletons
{
    public enum ViewType
    {
        Splitter,
        Converter,
        ProgressBar
    }

    public interface INavigator
    {
        ViewModelBase CurrentViewModel { get; set; }
        ViewModelBase ChildViewModel { get; set; }
        ICommand UpdateCurrentViewModelCommand { get; }
        ICommand OpenChildWindow { get; }
        ICommand CloseChildWindow { get; }
        void SetChildViewShown(bool shown);
    }

    public class Navigator : INavigator, INotifyPropertyChanged
    {
        private static readonly Lazy<Navigator> instance = new Lazy<Navigator>(() => new Navigator());
        public static Navigator Instance => instance.Value;

        private ViewModelBase currentViewModel;
        private ViewModelBase childViewModel;
        private bool childViewShown;
        private Window childView;


        public Navigator()
        {
            currentViewModel = new SplitterViewModel();
        }

        #region Properties
        public string SplitterLabel => "Splitter";
        public string ConverterLabel => "Converter";

        public ViewModelBase CurrentViewModel
        {
            get => currentViewModel;
            set
            {
                currentViewModel = value;
                OnPropertyChanged(nameof(CurrentViewModel));
            }
        }

        public ViewModelBase ChildViewModel
        {
            get => childViewModel;
            set
            {
                childViewModel = value;
                OnPropertyChanged(nameof(ChildViewModel));
            }
        }

        public bool ChildViewShown
        {
            get => childViewShown;
            set
            {
                childViewShown = value;
                OnPropertyChanged(nameof(ChildViewShown));
            }
        }

        public Window GetChildView() => childView;
        public void SetChildView(Window view) => childView = view;

        #endregion

        public void SetChildViewShown(bool shown) => ChildViewShown = shown;

        public ICommand UpdateCurrentViewModelCommand => new UpdateCurrentViewModelCommand(this);
        public ICommand OpenChildWindow => new OpenChildWindowCommand();
        public ICommand CloseChildWindow => new CloseChildWindowCommand();

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class UpdateCurrentViewModelCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;
        private INavigator navigator;

        public UpdateCurrentViewModelCommand(INavigator navigator)
        {
            this.navigator = navigator;
        }

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter)
        {
            if (parameter is ViewType viewType)
            {
                switch (viewType)
                {
                    case ViewType.Splitter:
                        navigator.CurrentViewModel = new SplitterViewModel();
                        break;
                    case ViewType.Converter:
                        navigator.CurrentViewModel = new ConverterViewModel();
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
        }
    }

    public class OpenChildWindowCommand : ICommand
    {

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter)
        {
            if (parameter is ViewType viewType)
            {
                switch (viewType)
                {
                    case ViewType.ProgressBar:
                        Navigator.Instance.ChildViewModel = new ProgressBarViewModel();
                        Navigator.Instance.SetChildViewShown(true);
                        Navigator.Instance.SetChildView(new ProgressBarView());
                        Navigator.Instance.GetChildView().Show();
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
        }
    }

    public class CloseChildWindowCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter)
        {
            if (parameter is ViewType viewType)
            {
                switch (viewType)
                {
                    case ViewType.ProgressBar:
                        Navigator.Instance.ChildViewModel = null;
                        Navigator.Instance.SetChildViewShown(false);
                        Navigator.Instance.GetChildView().Close();
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
        }
    }
}