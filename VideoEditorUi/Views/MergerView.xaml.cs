﻿using System.Windows;
using MVVMFramework.ViewNavigator;
using MVVMFramework.Views;
using VideoEditorUi.ViewModels;

namespace VideoEditorUi.Views
{
    /// <summary>
    /// Interaction logic for ConverterView.xaml
    /// </summary>
    public partial class MergerView : ViewBaseControl
    {
        public MergerView() : base(Navigator.Instance.CurrentViewModel)
        {
            InitializeComponent();

        }
    }
}
