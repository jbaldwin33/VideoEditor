using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Microsoft.Win32;
using MVVMFramework.ViewModels;
using MVVMFramework.ViewNavigator;
using VideoUtilities;
using Microsoft.WindowsAPICodePack.Dialogs;
using MVVMFramework;
using static VideoUtilities.Enums.Enums;

namespace VideoEditorUi.ViewModels
{
    public class MergerViewModel : ViewModel
    {
        private string outputPath;
        private decimal progressValue;
        private RelayCommand selectFileCommand;
        private RelayCommand mergeCommand;
        private RelayCommand moveUpCommand;
        private RelayCommand moveDownCommand;
        private RelayCommand removeCommand;
        private RelayCommand selectOutputFolderCommand;
        private ProgressBarViewModel progressBarViewModel;
        private VideoMerger merger;
        private string selectedFile;
        private bool fileSelected;
        private List<(string, string, string)> fileViewModels;
        private ObservableCollection<string> fileCollection;
        private bool canChangeExtension;
        private bool outputDifferentFormat;
        private bool multipleExtensions;
        private ObservableCollection<FormatTypeViewModel> formats;
        private FormatEnum formatType;

        public string OutputPath
        {
            get => outputPath;
            set => SetProperty(ref outputPath, value);
        }

        public decimal ProgressValue
        {
            get => progressValue;
            set => SetProperty(ref progressValue, value);
        }

        public ProgressBarViewModel ProgressBarViewModel
        {
            get => progressBarViewModel;
            set => SetProperty(ref progressBarViewModel, value);
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

        public List<(string folder, string filename, string extension)> FileViewModels
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

        public ObservableCollection<FormatTypeViewModel> Formats
        {
            get => formats;
            set => SetProperty(ref formats, value);
        }

        public FormatEnum FormatType
        {
            get => formatType;
            set => SetProperty(ref formatType, value);
        }


        public RelayCommand SelectFileCommand => selectFileCommand ?? (selectFileCommand = new RelayCommand(SelectFileCommandExecute, () => true));
        public RelayCommand MergeCommand => mergeCommand ?? (mergeCommand = new RelayCommand(MergeCommandExecute, MergeCommandCanExecute));
        public RelayCommand MoveUpCommand => moveUpCommand ?? (moveUpCommand = new RelayCommand(MoveUpExecute, () => FileSelected));
        public RelayCommand MoveDownCommand => moveDownCommand ?? (moveDownCommand = new RelayCommand(MoveDownExecute, () => FileSelected));
        public RelayCommand RemoveCommand => removeCommand ?? (removeCommand = new RelayCommand(RemoveExecute, () => FileSelected));

        public RelayCommand SelectOutputFolderCommand => selectOutputFolderCommand ?? (selectOutputFolderCommand = new RelayCommand(SelectOutputFolderCommandExecute, () => true));

        public string MergeLabel => Translatables.MergeLabel;
        public string SelectFileLabel => Translatables.SelectFileLabel;
        public string MoveUpLabel => Translatables.MoveUpLabel;
        public string MoveDownLabel => Translatables.MoveDownLabel;
        public string RemoveLabel => Translatables.RemoveLabel;
        public string OutputFormatLabel => Translatables.OutputFormatLabel;
        public string OutputFolderLabel => Translatables.OutputFolderLabel;
        private static readonly object _lock = new object();

        public MergerViewModel()
        {
            FileCollection = new ObservableCollection<string>();
            FileViewModels = new List<(string, string, string)>();
            Formats = FormatTypeViewModel.CreateViewModels();
            FileCollection.CollectionChanged += FileCollection_CollectionChanged;
            FormatType = FormatEnum.avi;

            BindingOperations.EnableCollectionSynchronization(FileCollection, _lock);
            BindingOperations.EnableCollectionSynchronization(FileViewModels, _lock);
        }

        public override void OnUnloaded()
        {
            FileCollection.CollectionChanged -= FileCollection_CollectionChanged;
            base.OnUnloaded();
        }

        private void FileCollection_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            MultipleExtensions = FileViewModels.Any(f => f.extension != FileViewModels[0].extension);
            if (FileViewModels.Any(f => f.extension.Contains(FormatEnum.mp4.ToString())))
            {
                CanChangeExtension = false;
                FormatType = FormatEnum.mp4;
                OutputDifferentFormat = true;
            }
            else
                CanChangeExtension = true;
        }

        private bool MergeCommandCanExecute() => FileCollection.Count > 1;

        private void SelectFileCommandExecute()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "All Video Files|*.wmv;*.avi;*.mpg;*.mpeg;*.mp4;*.mov;*.m4a;*.mkv;*.ts;*.WMV;*.AVI;*.MPG;*.MPEG;*.MP4;*.MOV;*.M4A;*.MKV;*.TS",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
            };

            if (openFileDialog.ShowDialog() == false)
                return;

            FileViewModels.Add(CreateFileViewModel(openFileDialog.FileName));
            FileCollection.Add(openFileDialog.SafeFileName);
        }

        private (string folder, string filename, string extension) CreateFileViewModel(string filename)
            => (Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename), Path.GetExtension(filename));

        private void MergeCommandExecute()
        {
            var outExt = FileViewModels.Any(f => f.extension.Contains("mp4"))
                ? ".mp4"
                : MultipleExtensions
                    ? $".{FormatType}"
                    : FileViewModels[0].extension;
            merger = new VideoMerger(FileViewModels, OutputPath, outExt);
            merger.StartedDownload += Merger_DownloadStarted;
            merger.ProgressDownload += Merger_ProgressDownload;
            merger.FinishedDownload += Merger_FinishedDownload;
            merger.ErrorDownload += Merger_ErrorDownload;
            ProgressBarViewModel = new ProgressBarViewModel();
            ProgressBarViewModel.OnCancelledHandler += (sender, args) =>
            {
                try
                {
                    merger.CancelOperation(string.Empty);
                }
                catch (Exception ex)
                {
                    ShowMessage(new MessageBoxEventArgs(ex.Message, MessageBoxEventArgs.MessageTypeEnum.Error, MessageBoxButton.OK, MessageBoxImage.Error));
                }
            };
            Task.Run(() => merger.MergeVideo());
            Navigator.Instance.OpenChildWindow.Execute(ProgressBarViewModel);
        }

        private void MoveUpExecute()
        {
            var index = FileCollection.IndexOf(SelectedFile);
            if (index == 0)
                return;
            var file = SelectedFile;
            FileCollection.Remove(SelectedFile);
            FileCollection.Insert(index - 1, file);
            SelectedFile = file;
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

        private void RemoveExecute()
        {
            FileViewModels.Remove(FileViewModels.First(f => f.filename.Contains(Path.GetFileNameWithoutExtension(SelectedFile))));
            FileCollection.Remove(SelectedFile);
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

        private void Merger_DownloadStarted(object sender, DownloadStartedEventArgs e) => ProgressBarViewModel.UpdateLabel(e.Label);

        private void Merger_ProgressDownload(object sender, ProgressEventArgs e)
        {
            if (e.Percentage > ProgressValue)
                ProgressBarViewModel.UpdateProgressValue(e.Percentage);
        }

        private void Merger_FinishedDownload(object sender, FinishedEventArgs e)
        {
            CleanUp();
            var message = e.Cancelled
                ? $"{Translatables.OperationCancelled} {e.Message}"
                : Translatables.VideoSuccessfullyMerged;
            ShowMessage(new MessageBoxEventArgs(message, MessageBoxEventArgs.MessageTypeEnum.Information, MessageBoxButton.OK, MessageBoxImage.Information));
        }

        private void Merger_ErrorDownload(object sender, ProgressEventArgs e)
        {
            Navigator.Instance.CloseChildWindow.Execute(false);
            ShowMessage(new MessageBoxEventArgs($"{Translatables.ErrorOccurred}\n\n{e.Error}", MessageBoxEventArgs.MessageTypeEnum.Error, MessageBoxButton.OK, MessageBoxImage.Error));
        }

        private void CleanUp()
        {
            FileCollection.Clear();
            FileViewModels.Clear();
            FormatType = FormatEnum.avi;
            OutputDifferentFormat = false;
            OutputPath = null;
            Navigator.Instance.CloseChildWindow.Execute(false);
        }
    }
}