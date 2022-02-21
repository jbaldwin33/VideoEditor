using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.Win32;
using MVVMFrameworkNet472.Localization;
using MVVMFrameworkNet472.ViewModels;
using MVVMFrameworkNet472.ViewNavigator;
using VideoEditorUi.Views;
using VideoUtilities;

namespace VideoEditorUi.ViewModels
{
    public class ChapterAdderViewModel : EditorViewModel
    {
        #region Fields and props

        private RelayCommand seekBackCommand;
        private RelayCommand playCommand;
        private RelayCommand seekForwardCommand;
        private RelayCommand startCommand;
        private RelayCommand endCommand;
        private RelayCommand addChapterCommand;
        private RelayCommand selectFileCommand;
        private RelayCommand importCommand;
        private RelayCommand rectCommand;
        private RelayCommand jumpToTimeCommand;
        private TimeSpan startTime;
        private TimeSpan endTime;
        private string startTimeString;
        private string currentTimeString;
        private bool startTimeSet;
        private string inputPath;
        private ObservableCollection<SectionViewModel> sectionViewModels;
        private ObservableCollection<RectClass> rectCollection;
        private string textInput;
        private bool timesImported;
        private bool deleteChapterFile;

        public TimeSpan StartTime
        {
            get => startTime;
            set
            {
                SetProperty(ref startTime, value);
                StartTimeString = StartTime.ToString("hh':'mm':'ss':'fff");
            }
        }

        public TimeSpan EndTime
        {
            get => endTime;
            set => SetProperty(ref endTime, value);
        }

        public string StartTimeString
        {
            get => startTimeString;
            set => SetProperty(ref startTimeString, value);
        }

        public string CurrentTimeString
        {
            get => currentTimeString;
            set => SetProperty(ref currentTimeString, value);
        }

        public bool StartTimeSet
        {
            get => startTimeSet;
            set => SetProperty(ref startTimeSet, value);
        }

        public string InputPath
        {
            get => inputPath;
            set => SetProperty(ref inputPath, value);
        }

        public ObservableCollection<SectionViewModel> SectionViewModels
        {
            get => sectionViewModels;
            set => SetProperty(ref sectionViewModels, value);
        }

        public ObservableCollection<RectClass> RectCollection
        {
            get => rectCollection;
            set => SetProperty(ref rectCollection, value);
        }

        public string TextInput
        {
            get => textInput;
            set => SetProperty(ref textInput, value);
        }

        public bool TimesImported
        {
            get => timesImported;
            set => SetProperty(ref timesImported, value);
        }

        public bool DeleteChapterFile
        {
            get => deleteChapterFile;
            set => SetProperty(ref deleteChapterFile, value);
        }

        #endregion

        #region  Labels

        public string StartLabel => new StartTimeLabelTranslatable();
        public string EndLabel => new EndTimeLabelTranslatable();
        public string AddChapterLabel => new AddChapterTranslatable();
        public string ConfirmLabel => new ConfirmTranslatable();
        public string CancelLabel => new CancelTranslatable();
        public string ImportLabel => new ImportLabelTranslatable();
        public string AddChaptersLabel => new AddChaptersMessageTranslatable();
        public string JumpToTimeLabel => new JumpToTimeLabelTranslatable();
        public string TagText => new EnterTitleTranslatable();
        public string DragFileLabel => new DragFileTranslatable();
        public string DeleteChapterFileLabel => new DeleteChapterFileLabelTranslatable();

        #endregion

        #region Commands

        public RelayCommand SeekBackCommand => seekBackCommand ?? (seekBackCommand = new RelayCommand(SeekBackCommandExecute, () => FileLoaded));
        public RelayCommand SeekForwardCommand => seekForwardCommand ?? (seekForwardCommand = new RelayCommand(SeekForwardCommandExecute, () => FileLoaded));
        public RelayCommand PlayCommand => playCommand ?? (playCommand = new RelayCommand(PlayCommandExecute, () => FileLoaded));
        public RelayCommand StartCommand => startCommand ?? (startCommand = new RelayCommand(StartCommandExecute, () => !StartTimeSet && FileLoaded && !TimesImported));
        public RelayCommand EndCommand => endCommand ?? (endCommand = new RelayCommand(EndCommandExecute, () => StartTimeSet && !TimesImported));
        public RelayCommand AddChapterCommand => addChapterCommand ?? (addChapterCommand = new RelayCommand(AddChapterCommandExecute, () => (SectionViewModels?.Count > 0 || TimesImported)));
        public RelayCommand SelectFileCommand => selectFileCommand ?? (selectFileCommand = new RelayCommand(SelectFileCommandExecute, () => true));
        public RelayCommand ImportCommand => importCommand ?? (importCommand = new RelayCommand(ImportCommandExecute, () => FileLoaded));
        public RelayCommand RectCommand => rectCommand ?? (rectCommand = new RelayCommand(RectCommandExecute, () => true));
        public RelayCommand JumpToTimeCommand => jumpToTimeCommand ?? (jumpToTimeCommand = new RelayCommand(JumpToTimeCommandExecute, () => FileLoaded));


        #endregion

        private string importedFile;
        private static readonly object _lock = new object();

        public override void OnUnloaded()
        {
            FileLoaded = false;
            TimesImported = false;
            base.OnUnloaded();
        }

        public override void Initialize()
        {
            StartTimeSet = false;
            StartTime = EndTime = TimeSpan.FromMilliseconds(0);
            CurrentTimeString = "00:00:00:000";
            PositionChanged = time => CurrentTimeString = time.ToString("hh':'mm':'ss':'fff");
            SectionViewModels = new ObservableCollection<SectionViewModel>();
            RectCollection = new ObservableCollection<RectClass>();

            BindingOperations.EnableCollectionSynchronization(RectCollection, _lock);
            BindingOperations.EnableCollectionSynchronization(SectionViewModels, _lock);
        }

        private void SeekBackCommandExecute()
        {
            SeekEvent(-5000);
            CurrentTimeString = new TimeSpan(0, 0, 0, 0, (int)SliderValue).ToString("hh':'mm':'ss':'fff");
        }
        private void SeekForwardCommandExecute()
        {
            SeekEvent(5000);
            CurrentTimeString = new TimeSpan(0, 0, 0, 0, (int)SliderValue).ToString("hh':'mm':'ss':'fff");
        }

        private void JumpToTimeCommandExecute()
        {
            TimeSpan.TryParseExact(CurrentTimeString, "hh':'mm':'ss':'fff", CultureInfo.CurrentCulture, out var result);
            SeekEvent(result.TotalMilliseconds);
            //SetPlayerPosition(SliderValue);
        }

        private void AddChapterCommandExecute()
        {
            if (SectionViewModels.Any(t => string.IsNullOrEmpty(t.Title)))
            {
                ShowMessage(new MessageBoxEventArgs(new InsufficientTitlesTranslatable(), MessageBoxEventArgs.MessageTypeEnum.Error, MessageBoxButton.OK, MessageBoxImage.Error));
                return;
            }

            var args = TimesImported 
                ? new ChapterAdderArgs(InputPath, importedFile, DeleteChapterFile)
                : new ChapterAdderArgs(InputPath, SectionViewModels.ToList(), DeleteChapterFile);
            Setup(true, true, args, null, Adder_GetMetadataFinished);
            Execute(StageEnum.Primary, new GettingMetadataMessageTranslatable());
        }

        private void StartCommandExecute()
        {
            StartTimeSet = true;
            StartTime = GetPlayerPosition();
        }
        private void EndCommandExecute()
        {
            if (GetPlayerPosition() <= StartTime)
            {
                StartTimeSet = false;
                StartTime = TimeSpan.FromMilliseconds(0);
                ShowMessage(new MessageBoxEventArgs(new EndTimeAfterStartTimeTranslatable(), MessageBoxEventArgs.MessageTypeEnum.Error, MessageBoxButton.OK, MessageBoxImage.Error));
                return;
            }
            EndTime = GetPlayerPosition();
            AddRectangle();

            new ChapterTitleDialogView { DataContext = this, Title = new AddChapterTitleTranslatable(), WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = Application.Current.MainWindow }.ShowDialog();
            TextInput = TextInput.Replace(',', '_').Replace(':', '_');
            SectionViewModels.Add(new SectionViewModel(StartTime, EndTime, TextInput));
            TextInput = string.Empty;
            StartTimeSet = false;
            StartTime = EndTime = TimeSpan.FromMilliseconds(0);
        }

        private void SelectFileCommandExecute()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "All Video Files|*.wmv;*.avi;*.mpg;*.mpeg;*.mp4;*.mov;*.m4a;*.mkv;*.ts;*.WMV;*.AVI;*.MPG;*.MPEG;*.MP4;*.MOV;*.M4A;*.MKV;*.TS",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
            };

            if (openFileDialog.ShowDialog() == false)
                return;

            //InputPath = openFileDialog.FileName;
            //UtilityClass.GetDetails(Player, openFileDialog.FileName);
            //Player.Open(new Uri(openFileDialog.FileName));
            //FileLoaded = true;
            //ResetAll();

            //if (!canAddChapters())
            //    ShowMessage(new MessageBoxEventArgs(new ChapterMarkerCompatibleFormatsTranslatable(string.Join(", ", FormatTypeViewModel.ChapterMarkerCompatibleFormats.Select(f => f.ToString()))), MessageBoxEventArgs.MessageTypeEnum.Information, MessageBoxButton.OK, MessageBoxImage.Information));

            bool canAddChapters() => FormatTypeViewModel.ChapterMarkerCompatibleFormats.Contains((Enums.FormatEnum)Enum.Parse(typeof(Enums.FormatEnum), Path.GetExtension(openFileDialog.FileName).Substring(1)));
        }

        private void ImportCommandExecute()
        {
            ShowMessage(new MessageBoxEventArgs($"{new ChapterFileFormatMessageTranslatable()}\nStartTime,Title\n00:00:00,Chapter 1\n00:30:14,Chapter 2", MessageBoxEventArgs.MessageTypeEnum.Information, MessageBoxButton.OK, MessageBoxImage.Information));
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Text Files|*.txt",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            };

            if (openFileDialog.ShowDialog() == false)
                return;

            ResetAll();
            importedFile = openFileDialog.FileName;
            TimesImported = true;
        }

        private void RectCommandExecute(object obj)
        {
            var rect = obj as RectClass;
            var args = new MessageBoxEventArgs(new DeleteSectionConfirmTranslatable(), MessageBoxEventArgs.MessageTypeEnum.Information, MessageBoxButton.YesNo, MessageBoxImage.Question);
            ShowMessage(args);
            if (args.Result != MessageBoxResult.Yes)
                return;

            var index = RectCollection.IndexOf(rect);
            RectCollection.Remove(rect);
            SectionViewModels.RemoveAt(index);
        }

        private void AddRectangle()
        {
            var rect = new RectClass
            {
                RectCommand = RectCommand,
                Margin = new Thickness(mapToRange(StartTime.TotalMilliseconds, 760, SliderMax), 0, 0, 0),
                Width = mapToRange((EndTime - StartTime).TotalMilliseconds, 760, SliderMax),
                Height = 5,
                HorizontalAlignment = HorizontalAlignment.Left,
                Fill = new SolidColorBrush(Colors.Red)
            };
            RectCollection.Add(rect);
            double mapToRange(double toConvert, double maxRange1, double maxRange2) => toConvert * (maxRange1 / maxRange2);
        }

        private void ClearAllRectangles()
        {
            RectCollection.Clear();
            SectionViewModels.Clear();
        }

        private void ResetAll()
        {
            StartTimeSet = false;
            StartTime = EndTime = TimeSpan.FromMilliseconds(0);
            CurrentTimeString = "00:00:00:000";
            TimesImported = false;
            importedFile = string.Empty;
            ClearAllRectangles();
        }

        protected override void FinishedDownload(object sender, FinishedEventArgs e)
        {
            base.FinishedDownload(sender, e);
            var message = e.Cancelled
                ? $"{new OperationCancelledTranslatable()} {e.Message}"
                : new ChaptersSuccessfullyAddedTranslatable();
            ShowMessage(new MessageBoxEventArgs(message, MessageBoxEventArgs.MessageTypeEnum.Information, MessageBoxButton.OK, MessageBoxImage.Information));
        }

        protected override void ErrorDownload(object sender, ProgressEventArgs e)
        {
            base.ErrorDownload(sender, e);
            ShowMessage(new MessageBoxEventArgs($"{new ChapterAdderTryAgainTranslatable(Path.GetDirectoryName(InputPath))}", MessageBoxEventArgs.MessageTypeEnum.Error, MessageBoxButton.OK, MessageBoxImage.Error));
        }

        private void Adder_GetMetadataFinished(object sender, EventArgs e)
        {
            UtilityClass.CloseChildWindow(false);
            Setup(false, false, null, null, null);
            Execute(StageEnum.Secondary, null);
        }

        public override void CleanUp(bool isError)
        {
            if (!isError)
            {
                FileLoaded = false;
                ResetAll();
            }
            base.CleanUp(isError);
        }
    }
}
