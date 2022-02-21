using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using Microsoft.Win32;
using MVVMFramework.ViewModels;
using VideoUtilities;
using Microsoft.WindowsAPICodePack.Dialogs;
using MVVMFramework.Localization;
using static VideoUtilities.Enums;
using System.Windows.Input;

namespace VideoEditorUi.ViewModels
{
    public class MergerViewModel : EditorViewModel
    {
        #region Fields and props

        private string outputPath;
        private RelayCommand selectFileCommand;
        private RelayCommand mergeCommand;
        private RelayCommand moveUpCommand;
        private RelayCommand moveDownCommand;
        private RelayCommand removeCommand;
        private RelayCommand selectOutputFolderCommand;
        private string selectedFile;
        private bool fileSelected;
        private List<string> fileViewModels;
        private ObservableCollection<string> fileCollection;
        private bool canChangeExtension;
        private bool outputDifferentFormat;
        private bool multipleExtensions;
        private List<FormatTypeViewModel> formats;
        private FormatEnum formatType;

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

        public List<string> FileViewModels
        {
            get => fileViewModels;
            set => SetProperty(ref fileViewModels, value);
        }

        public bool CanChangeExtension
        {
            get => canChangeExtension;
            set => SetProperty(ref canChangeExtension, value);
        }

        public bool OutputDifferentFormat
        {
            get => outputDifferentFormat;
            set => SetProperty(ref outputDifferentFormat, value);
        }

        public bool MultipleExtensions
        {
            get => multipleExtensions;
            set => SetProperty(ref multipleExtensions, value);
        }

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

        #endregion

        #region Commands

        public RelayCommand SelectFileCommand => selectFileCommand ?? (selectFileCommand = new RelayCommand(SelectFileCommandExecute, () => true));
        public RelayCommand MergeCommand => mergeCommand ?? (mergeCommand = new RelayCommand(MergeCommandExecute, () => FileCollection?.Count > 1));
        public RelayCommand MoveUpCommand => moveUpCommand ?? (moveUpCommand = new RelayCommand(MoveUpExecute, () => FileSelected));
        public RelayCommand MoveDownCommand => moveDownCommand ?? (moveDownCommand = new RelayCommand(MoveDownExecute, () => FileSelected));
        public RelayCommand RemoveCommand => removeCommand ?? (removeCommand = new RelayCommand(RemoveExecute, () => FileSelected));
        public RelayCommand SelectOutputFolderCommand => selectOutputFolderCommand ?? (selectOutputFolderCommand = new RelayCommand(SelectOutputFolderCommandExecute, () => true));

        #endregion

        #region Labels

        public string MergeLabel => new MergeLabelTranslatable();
        public string SelectFileLabel => new SelectFileLabelTranslatable();
        public string MoveUpLabel => new MoveUpLabelTranslatable();
        public string MoveDownLabel => new MoveDownLabelTranslatable();
        public string RemoveLabel => new RemoveSelectedLabelTranslatable();
        public string OutputFormatLabel => new OutputFormatQuestionTranslatable();
        public string OutputFolderLabel => new OutputFolderLabelTranslatable();

        #endregion

        private static readonly object _lock = new object();

        public override void OnUnloaded()
        {
            FileCollection.CollectionChanged -= FileCollection_CollectionChanged;
            FileCollection.Clear();
            FileViewModels.Clear();
            base.OnUnloaded();
        }

        public override void Initialize()
        {
            FileCollection = new ObservableCollection<string>();
            FileViewModels = new List<string>();
            Formats = FormatTypeViewModel.CreateViewModels();
            FileCollection.CollectionChanged += FileCollection_CollectionChanged;
            FormatType = FormatEnum.avi;
            BindingOperations.EnableCollectionSynchronization(FileCollection, _lock);
            BindingOperations.EnableCollectionSynchronization(FileViewModels, _lock);
        }

        protected override void DragFilesCallback(string[] files)
        {
            FileViewModels.AddRange(files);
            var safeFileNames = files.Select(Path.GetFileName);
            foreach (var file in safeFileNames)
                FileCollection.Add(file);
            CommandManager.InvalidateRequerySuggested();
        }

        private void FileCollection_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            MultipleExtensions = FileViewModels.Any(f => Path.GetExtension(f) != Path.GetExtension(FileViewModels[0]));
            if (FileViewModels.Any(f => Path.GetExtension(f).Contains(FormatEnum.mp4.ToString())))
            {
                CanChangeExtension = false;
                FormatType = FormatEnum.mp4;
                OutputDifferentFormat = true;
            }
            else
            {
                CanChangeExtension = true;
                FormatType = FormatEnum.avi;
                OutputDifferentFormat = false;
            }
        }

        private void SelectFileCommandExecute()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "All Video Files|*.wmv;*.avi;*.mpg;*.mpeg;*.mp4;*.mov;*.m4a;*.mkv;*.ts;*.txt;*.WMV;*.AVI;*.MPG;*.MPEG;*.MP4;*.MOV;*.M4A;*.MKV;*.TS",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == false)
                return;

            FileViewModels.AddRange(openFileDialog.FileNames);
            foreach (var file in openFileDialog.SafeFileNames)
                FileCollection.Add(file);
        }

        private (string folder, string filename, string extension) CreateFileViewModel(string filename)
            => (Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename), Path.GetExtension(filename));

        private void MergeCommandExecute()
        {
            if (string.IsNullOrEmpty(OutputPath))
            {
                ShowMessage(new MessageBoxEventArgs(new SelectOutputFolderTranslatable(), MessageBoxEventArgs.MessageTypeEnum.Information, MessageBoxButton.OK, MessageBoxImage.Information));
                return;
            }
            var outExt = FileViewModels.Any(f => Path.GetExtension(f).Contains("mp4"))
                ? ".mp4"
                : MultipleExtensions
                    ? $".{FormatType}"
                    : Path.GetExtension(FileViewModels[0]);
            var args = new MergerArgs(FileViewModels, OutputPath, outExt);
            //VideoEditor = new VideoMerger(FileViewModels, OutputPath, outExt);
            Setup(false, false, args, null, null);
            Execute(StageEnum.Pre, null);
        }

        private void MoveUpExecute()
        {
            var index = FileCollection.IndexOf(SelectedFile);
            if (index == 0)
                return;
            var ff = FileViewModels.First(f => Path.GetFileNameWithoutExtension(f) == Path.GetFileNameWithoutExtension(SelectedFile));
            var fileWithExt = Path.GetFileName(ff);// $"{file}{ext}";
            FileCollection.Remove(fileWithExt);
            FileViewModels.Remove(ff);//(path, file, ext));
            FileCollection.Insert(index - 1, fileWithExt);
            FileViewModels.Insert(index - 1, ff);//(path, file, ext));
            SelectedFile = fileWithExt;
        }
        private void MoveDownExecute()
        {
            var index = FileCollection.IndexOf(SelectedFile);
            if (index == FileCollection.Count - 1)
                return;
            var file = SelectedFile;
            FileCollection.Remove(SelectedFile);
            FileCollection.Insert(index + 1, file);
            SelectedFile = file;
        }

        private void RemoveExecute(object param)
        {
            var items = ((System.Collections.IList)param).Cast<string>().ToList();
            for (var i = 0; i < items.Count; i++)
            {
                FileViewModels.Remove(FileViewModels.First(f => Path.GetFileNameWithoutExtension(f).Contains(Path.GetFileNameWithoutExtension(items[i]))));
                FileCollection.Remove(items[i]);
            }
        }

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
            base.FinishedDownload(sender, e);
            var message = e.Cancelled
                ? $"{new OperationCancelledTranslatable()} {e.Message}"
                : new VideoSuccessfullyMergedTranslatable();
            ShowMessage(new MessageBoxEventArgs(message, MessageBoxEventArgs.MessageTypeEnum.Information, MessageBoxButton.OK, MessageBoxImage.Information));
        }

        public override void CleanUp(bool isError)
        {
            if (!isError)
            {
                FileCollection.Clear();
                FileViewModels.Clear();
                FormatType = FormatEnum.avi;
                OutputDifferentFormat = false;
                OutputPath = null;
            }
            base.CleanUp(isError);
        }
    }
}