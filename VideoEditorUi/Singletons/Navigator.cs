using GalaSoft.MvvmLight;
using System;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows;
using VideoEditorUi.ViewModels;

namespace VideoEditorUi.Singletons
{
    public enum ViewType
    {
        Splitter,
        Converter
    }

    public interface INavigator
    {
        ViewModelBase CurrentViewModel { get; set; }
        ICommand UpdateCurrentViewModelCommand { get; }
    }

    public class Navigator : INavigator, INotifyPropertyChanged
    {
        private static readonly Lazy<Navigator> instance = new Lazy<Navigator>(() => new Navigator());
        public static Navigator Instance => instance.Value;

        private ViewModelBase currentViewModel;

        public Navigator()
        {
            currentViewModel = new SplitterViewModel();
        }

        #region Properties
        public string SplitterLabel => "Splitter";
        public string ConverterLabel => "Converter";
        //public string UserDetailsLabel => new UserDetailsLabelTranslatable();
        //public string LoginLabel => new LoginLabelTranslatable();
        //public string SignUpLabel => new SignUpLabelTranslatable();
        //public string TransactionsLabel => new TransactionsLabelTranslatable();

        public ViewModelBase CurrentViewModel
        {
            get => currentViewModel;
            set
            {
                currentViewModel = value;
                OnPropertyChanged(nameof(CurrentViewModel));
            }
        }


        #endregion

        public ICommand UpdateCurrentViewModelCommand => new UpdateCurrentViewModelCommand(this);

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
}