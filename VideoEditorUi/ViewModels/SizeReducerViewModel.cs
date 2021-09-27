using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using MVVMFramework.Localization;
using MVVMFramework.ViewModels;
using VideoUtilities;
using static VideoUtilities.Enums;

namespace VideoEditorUi.ViewModels
{
    public class SizeReducerViewModel : EditorViewModel
    {
        #region Fields and props

        private List<FormatTypeViewModel> formats;
        private FormatEnum formatType;
        private string outputPath;
        private RelayCommand selectFileCommand;
        private RelayCommand reduceSizeCommand;
        private RelayCommand convertCommand;
        private RelayCommand removeCommand;
        private RelayCommand selectOutputFolderCommand;
        private string selectedFile;
        private bool fileSelected;
        private ObservableCollection<string> fileCollection;
        private bool converterSelected;

        public List<FormatTypeViewModel> Formats
        {
            get => formats;
            set => SetProperty(ref formats, value);
        }

        public FormatEnum FormatType
        {
            get => formatType;
            set => SetProperty(ref formatType, value);
        }

        public string OutputPath
        {
            get => outputPath;
            set => SetProperty(ref outputPath, value);
        }

        public ObservableCollection<string> FileCollection
        {
            get => fileCollection;
            set => SetProperty(ref fileCollection, value);
        }

        public string SelectedFile
        {
            get => selectedFile;
            set
            {
                SetProperty(ref selectedFile, value);
                FileSelected = !string.IsNullOrEmpty(value);
            }
        }

        public bool FileSelected
        {
            get => fileSelected;
            set => SetProperty(ref fileSelected, value);
        }

        public bool ConverterSelected
        {
            get => converterSelected;
            set => SetProperty(ref converterSelected, value);
        }

        #endregion

        #region Commands

        public RelayCommand SelectFileCommand => selectFileCommand ?? (selectFileCommand = new RelayCommand(SelectFileCommandExecute, () => true));
        public RelayCommand ReduceSizeCommand => reduceSizeCommand ?? (reduceSizeCommand = new RelayCommand(ReduceSizeCommandExecute, () => FileCollection?.Count > 0));
        public RelayCommand ConvertCommand => convertCommand ?? (convertCommand = new RelayCommand(ConvertCommandExecute, () => FileCollection?.Count > 0));
        public RelayCommand RemoveCommand => removeCommand ?? (removeCommand = new RelayCommand(RemoveExecute, () => FileSelected));
        public RelayCommand SelectOutputFolderCommand => selectOutputFolderCommand ?? (selectOutputFolderCommand = new RelayCommand(SelectOutputFolderCommandExecute, () => true));

        #endregion

        #region Labels

        public string MergeLabel => new MergeLabelTranslatable();
        public string SelectFileLabel => new SelectFileLabelTranslatable();
        public string MoveUpLabel => new MoveUpLabelTranslatable();
        public string MoveDownLabel => new MoveDownLabelTranslatable();
        public string RemoveLabel => new RemoveLabelTranslatable();
        public string OutputFormatLabel => $"{new OutputFormatLabelTranslatable()}:";
        public string OutputFolderLabel => new OutputFolderLabelTranslatable();
        public string ReduceVideoSizeLabel => new ReduceVideoSizeLabelTranslatable();
        public string ConvertLabel => new ConvertLabelTranslatable();
        public string ConvertFormatLabel => new ConvertFormatLabelTranslatable();

        #endregion

        private static readonly object _lock = new object();

        public override void OnUnloaded()
        {
            FileCollection.Clear();
            base.OnUnloaded();
        }

        protected override void Initialize()
        {
            ConverterSelected = true;
            Formats = FormatTypeViewModel.CreateViewModels();
            FormatType = FormatEnum.avi;
            FileCollection = new ObservableCollection<string>();
            BindingOperations.EnableCollectionSynchronization(FileCollection, _lock);
        }

        protected override void DragFilesCallback(string[] files)
        {
            files.ToList().ForEach(FileCollection.Add);
            CommandManager.InvalidateRequerySuggested();
        }

        private void SelectFileCommandExecute()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "All Video Files|*.wmv;*.avi;*.mpg;*.mpeg;*.mp4;*.mov;*.m4a;*.mkv;*.ts;*.webm;*.WMV;*.AVI;*.MPG;*.MPEG;*.MP4;*.MOV;*.M4A;*.MKV;*.TS;*.WEBM",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == false)
                return;

            openFileDialog.FileNames.ToList().ForEach(FileCollection.Add);
        }

        private void ReduceSizeCommandExecute()
        {
            if (string.IsNullOrEmpty(OutputPath))
            {
                ShowMessage(new MessageBoxEventArgs(new SelectOutputFolderTranslatable(), MessageBoxEventArgs.MessageTypeEnum.Information, MessageBoxButton.OK, MessageBoxImage.Information));
                return;
            }
            VideoEditor = new VideoSizeReducer(FileCollection.Select(f => (Path.GetDirectoryName(f), Path.GetFileNameWithoutExtension(f), Path.GetExtension(f))), OutputPath);
            Setup(true, FileCollection.Count);
            Execute(StageEnum.Primary, new ReducingSizeLabelTranslatable());
        }

        private void ConvertCommandExecute()
        {
            if (string.IsNullOrEmpty(OutputPath))
            {
                ShowMessage(new MessageBoxEventArgs(new SelectOutputFolderTranslatable(), MessageBoxEventArgs.MessageTypeEnum.Information, MessageBoxButton.OK, MessageBoxImage.Information));
                return;
            }
            VideoEditor = new VideoConverter(FileCollection.Select(f => (Path.GetDirectoryName(f), Path.GetFileNameWithoutExtension(f), Path.GetExtension(f))), $".{FormatType}");
            Setup(true, FileCollection.Count);
            Execute(StageEnum.Primary, new ConvertingLabelTranslatable());
        }
        private void RemoveExecute() => FileCollection.Remove(SelectedFile);

        private void SelectOutputFolderCommandExecute()
        {
            var openFolderDialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
            };

            if (openFolderDialog.ShowDialog() == CommonFileDialogResult.Cancel)
                return;

            OutputPath = openFolderDialog.FileName;
        }

        protected override void FinishedDownload(object sender, FinishedEventArgs e)
        {
            if (!ConverterSelected)
                ProgressBarViewModel.SetFinished(e.ProcessIndex);
            base.FinishedDownload(sender, e);
            var message = e.Cancelled
                ? $"{new OperationCancelledTranslatable()} {e.Message}"
                : ConverterSelected ? (Translatable)new VideoSuccessfullyConvertedTranslatable() : new SizeSuccessfullyReducedTranslatable();
            ShowMessage(new MessageBoxEventArgs(message, MessageBoxEventArgs.MessageTypeEnum.Information, MessageBoxButton.OK, MessageBoxImage.Information));
        }

        protected override void CleanUp()
        {
            FileCollection.Clear();
            FormatType = FormatEnum.avi;
            OutputPath = null;
            base.CleanUp();
        }
    }
}