﻿using GalaSoft.MvvmLight;
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
        BaseViewModel CurrentViewModel { get; set; }
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

        private BaseViewModel currentViewModel;
        private ViewModelBase childViewModel;
        private bool childViewShown;
        private PopupWindowView childView;


        public Navigator()
        {
            currentViewModel = new SplitterViewModel();
        }

        #region Properties
        public string SplitterLabel => "Splitter";
        public string ConverterLabel => "Converter";

        public BaseViewModel CurrentViewModel
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

        public PopupWindowView ChildView
        {
            get => childView;
            set
            {
                childView = value;
                OnPropertyChanged(nameof(ChildView));
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
        private readonly INavigator navigator;

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
            //var view = Navigator.Instance.ChildView;
            Navigator.Instance.ChildView = new PopupWindowView
            {
                DataContext = new PopupWindowViewModel(Navigator.Instance),
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            Navigator.Instance.ChildViewModel = new ProgressBarViewModel();
            Navigator.Instance.SetChildViewShown(true);
            Navigator.Instance.ChildView.Owner = Application.Current.MainWindow;
            Navigator.Instance.ChildView.Show();
        }
    }

    public class CloseChildWindowCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter)
        {
            var vm = Navigator.Instance.CurrentViewModel;
            if ((bool)parameter)
            {
                switch (vm)
                {
                    case SplitterViewModel svm:
                        svm.CancelOperation();
                        break;
                    case ConverterViewModel cvm:
                        cvm.CancelOperation();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(vm), vm.GetType(), null);
                }
            }

            Navigator.Instance.ChildViewModel = null;
            Navigator.Instance.SetChildViewShown(false);
            Application.Current.Dispatcher.Invoke(() => Navigator.Instance.ChildView.Close());
        }
    }
}