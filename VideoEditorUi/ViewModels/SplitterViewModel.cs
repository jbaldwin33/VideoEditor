using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using CSVideoPlayer;
using MVVMFramework;
using MVVMFramework.ViewModels;
using MVVMFramework.ViewNavigator;
using VideoEditorUi.Views;
using VideoUtilities;
using static VideoUtilities.Enums;
using static VideoEditorUi.Utilities.UtilityClass;
using Path = System.IO.Path;

namespace VideoEditorUi.ViewModels
{
    public class SplitterViewModel : ViewModel
    {
        private RelayCommand seekBackCommand;
        private RelayCommand playCommand;
        private RelayCommand seekForwardCommand;
        private RelayCommand pauseCommand;
        private RelayCommand stopCommand;
        private RelayCommand startCommand;
        private RelayCommand endCommand;
        private RelayCommand splitCommand;
        private RelayCommand addChapterCommand;
        private RelayCommand selectFileCommand;
        private RelayCommand importCommand;
        private VideoPlayerWPF player;
        private Slider slider;
        private TimeSpan startTime;
        private TimeSpan endTime;
        private string startTimeString;
        private string currentTimeString;
        private bool startTimeSet;
        private bool endTimeSet;
        private bool fileLoaded;
        private bool reEncodeVideo;
        private string inputPath;
        private ObservableCollection<ChapterMarkerViewModel> chapterMarkers;
        private ObservableCollection<Rectangle> rectCollection;
        private List<FormatTypeViewModel> formats;
        private FormatEnum formatType;
        private bool canCombine;
        private bool combineVideo;
        private bool outputDifferentFormat;
        private ProgressBarViewModel progressBarViewModel;
        private bool addChapters;
        private string newChapter;
        private bool timesImported;

        public Slider Slider
        {
            get => slider;
            set => SetProperty(ref slider, value);
        }

        public VideoPlayerWPF Player
        {
            get => player;
            set => SetProperty(ref player, value);
        }

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

        public bool EndTimeSet
        {
            get => endTimeSet;
            set => SetProperty(ref endTimeSet, value);
        }

        public bool FileLoaded
        {
            get => fileLoaded;
            set => SetProperty(ref fileLoaded, value);
        }
        public bool ReEncodeVideo
        {
            get => reEncodeVideo;
            set => SetProperty(ref reEncodeVideo, value);
        }

        public string InputPath
        {
            get => inputPath;
            set => SetProperty(ref inputPath, value);
        }

        public ObservableCollection<ChapterMarkerViewModel> ChapterMarkers
        {
            get => chapterMarkers;
            set => SetProperty(ref chapterMarkers, value);
        }


        public ObservableCollection<Rectangle> RectCollection
        {
            get => rectCollection;
            set => SetProperty(ref rectCollection, value);
        }

        public FormatEnum FormatType
        {
            get => formatType;
            set => SetProperty(ref formatType, value);
        }

        public List<FormatTypeViewModel> Formats
        {
            get => formats;
            set => SetProperty(ref formats, value);
        }

        public bool CanCombine
        {
            get => canCombine;
            set => SetProperty(ref canCombine, value);
        }

        public bool CombineVideo
        {
            get => combineVideo;
            set => SetProperty(ref combineVideo, value);
        }

        public bool OutputDifferentFormat
        {
            get => outputDifferentFormat;
            set
            {
                SetProperty(ref outputDifferentFormat, value);
                ReEncodeVideo = value;
            }
        }

        public ProgressBarViewModel ProgressBarViewModel
        {
            get => progressBarViewModel;
            set => SetProperty(ref progressBarViewModel, value);
        }

        public bool AddChapters
        {
            get => addChapters;
            set
            {
                SetProperty(ref addChapters, value);
                CanCombine = !value && ChapterMarkers?.Count > 0;
            }
        }

        public string NewChapter
        {
            get => newChapter;
            set => SetProperty(ref newChapter, value);
        }

        public bool TimesImported
        {
            get => timesImported;
            set => SetProperty(ref timesImported, value);
        }


        public Action<TimeSpan> PositionChanged;

        #region  Labels

        public string PlayLabel => Translatables.PlayLabel;
        public string SeekBackLabel => Translatables.SeekBackLabel;
        public string SeekForwardLabel => Translatables.SeekForwardLabel;
        public string PauseLabel => Translatables.PauseLabel;
        public string StopLabel => Translatables.StopLabel;
        public string StartLabel => Translatables.StartTimeLabel;
        public string EndLabel => Translatables.EndTimeLabel;
        public string SplitLabel => Translatables.SplitLabel;
        public string SelectFileLabel => Translatables.SelectFileLabel;
        public string CombineSectionsLabel => Translatables.CombineSectionsQuestion;
        public string OutputFormatLabel => Translatables.OutputFormatQuestion;
        public string ReEncodeQuestionLabel => Translatables.ReEncodeQuestion;
        public string ReEncodeComment => Translatables.ReEncodeComment;
        public string AddChapterLabel => Translatables.AddChapter;
        public string ConfirmLabel => Translatables.Confirm;
        public string ImportLabel => Translatables.ImportLabel;

        #endregion

        public RelayCommand SeekBackCommand => seekBackCommand ?? (seekBackCommand = new RelayCommand(SeekBackCommandExecute, () => FileLoaded));
        public RelayCommand SeekForwardCommand => seekForwardCommand ?? (seekForwardCommand = new RelayCommand(SeekForwardCommandExecute, () => FileLoaded));
        public RelayCommand PlayCommand => playCommand ?? (playCommand = new RelayCommand(PlayCommandExecute, () => FileLoaded));
        public RelayCommand PauseCommand => pauseCommand ?? (pauseCommand = new RelayCommand(PauseCommandExecute, () => FileLoaded));
        public RelayCommand StopCommand => stopCommand ?? (stopCommand = new RelayCommand(StopCommandExecute, () => FileLoaded));
        public RelayCommand StartCommand => startCommand ?? (startCommand = new RelayCommand(StartCommandExecute, () => !StartTimeSet && FileLoaded && !TimesImported));
        public RelayCommand EndCommand => endCommand ?? (endCommand = new RelayCommand(EndCommandExecute, () => StartTimeSet && !EndTimeSet && !TimesImported));
        public RelayCommand SplitCommand => splitCommand ?? (splitCommand = new RelayCommand(SplitCommandExecute, () => ChapterMarkers?.Count > 0 && !AddChapters));
        public RelayCommand AddChapterCommand => addChapterCommand ?? (addChapterCommand = new RelayCommand(AddChapterCommandExecute, () => (ChapterMarkers?.Count > 0 || TimesImported) && AddChapters));
        public RelayCommand SelectFileCommand => selectFileCommand ?? (selectFileCommand = new RelayCommand(SelectFileCommandExecute, () => true));
        public RelayCommand ImportCommand => importCommand ?? (importCommand = new RelayCommand(ImportCommandExecute, () => FileLoaded));

        private VideoSplitter splitter;
        private VideoChapterAdder chapterAdder;
        private string importedFile;
        private static readonly object _lock = new object();

        public SplitterViewModel() { }

        public override void OnLoaded()
        {
            Initialize();
            base.OnLoaded();
        }

        public override void OnUnloaded()
        {
            ClosePlayer(player);
            FileLoaded = false;
            TimesImported = false;
            ChapterMarkers.CollectionChanged -= Times_CollectionChanged;
            base.OnUnloaded();
        }

        private void Initialize()
        {
            CanCombine = false;
            AddChapters = false;
            StartTimeSet = EndTimeSet = false;
            StartTime = EndTime = TimeSpan.FromMilliseconds(0);
            CurrentTimeString = "00:00:00:000";
            PositionChanged = time => CurrentTimeString = time.ToString("hh':'mm':'ss':'fff");
            ChapterMarkers = new ObservableCollection<ChapterMarkerViewModel>();
            RectCollection = new ObservableCollection<Rectangle>();
            Formats = FormatTypeViewModel.CreateViewModels();
            FormatType = FormatEnum.avi;
            ChapterMarkers.CollectionChanged += Times_CollectionChanged;

            BindingOperations.EnableCollectionSynchronization(RectCollection, _lock);
            BindingOperations.EnableCollectionSynchronization(ChapterMarkers, _lock);
        }

        private void Times_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => CanCombine = ChapterMarkers.Count > 1;

        private void Rect_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var args = new MessageBoxEventArgs(Translatables.DeleteSectionConfirm, MessageBoxEventArgs.MessageTypeEnum.Information, MessageBoxButton.YesNo, MessageBoxImage.Question);
            ShowMessage(args);
            if (args.Result == MessageBoxResult.Yes)
            {
                var rect = sender as Rectangle;
                var index = RectCollection.IndexOf(rect);
                RectCollection.Remove(rect);
                ChapterMarkers.RemoveAt(index);
            }
        }

        private void PlayCommandExecute() => player.Play();
        private void SeekBackCommandExecute()
        {
            slider.Value = slider.Value - 5000 < 0 ? 0 : slider.Value - 5000;
            SetPlayerPosition(player, slider.Value);
            CurrentTimeString = new TimeSpan(0, 0, 0, 0, (int)slider.Value).ToString("hh':'mm':'ss':'fff");
        }
        private void SeekForwardCommandExecute()
        {
            slider.Value = slider.Value + 5000 > slider.Maximum ? slider.Maximum : slider.Value + 5000;
            SetPlayerPosition(player, slider.Value);
            CurrentTimeString = new TimeSpan(0, 0, 0, 0, (int)slider.Value).ToString("hh':'mm':'ss':'fff");
        }

        private void PauseCommandExecute() => player.Pause();

        private void StopCommandExecute() => player.Stop();
        private void SplitCommandExecute()
        {
            splitter = new VideoSplitter(InputPath, ChapterMarkers.Select(t => new Tuple<TimeSpan, TimeSpan, string>(t.StartTime, t.EndTime, t.Title)).ToList(), CombineVideo, OutputDifferentFormat, $".{FormatType}", ReEncodeVideo);
            splitter.StartedDownload += Splitter_StartedDownload;
            splitter.ProgressDownload += Splitter_ProgressDownload;
            splitter.FinishedDownload += Splitter_FinishedDownload;
            splitter.ErrorDownload += Splitter_ErrorDownload;
            ProgressBarViewModel = new ProgressBarViewModel();
            ProgressBarViewModel.OnCancelledHandler += (sender, args) =>
            {
                try
                {
                    splitter.CancelOperation(string.Empty);
                }
                catch (Exception ex)
                {
                    ShowMessage(new MessageBoxEventArgs(ex.Message, MessageBoxEventArgs.MessageTypeEnum.Error, MessageBoxButton.OK, MessageBoxImage.Error));
                }
            };
            Task.Run(() => splitter.Split());
            Navigator.Instance.OpenChildWindow.Execute(ProgressBarViewModel);
        }

        private void AddChapterCommandExecute()
        {
            if (ChapterMarkers.Any(t => string.IsNullOrEmpty(t.Title)))
            {
                ShowMessage(new MessageBoxEventArgs(Translatables.InsufficientTitles, MessageBoxEventArgs.MessageTypeEnum.Error, MessageBoxButton.OK, MessageBoxImage.Error));
                return;
            }

            chapterAdder = TimesImported
                ? new VideoChapterAdder(InputPath, importChapterFile: importedFile)
                : new VideoChapterAdder(InputPath, ChapterMarkers.Select(t => new Tuple<TimeSpan, TimeSpan, string>(t.StartTime, t.EndTime, t.Title)).ToList());
            chapterAdder.StartedDownload += Splitter_StartedDownload;
            chapterAdder.ProgressDownload += Splitter_ProgressDownload;
            chapterAdder.FinishedDownload += Splitter_FinishedDownload;
            chapterAdder.ErrorDownload += Splitter_ErrorDownload;
            ProgressBarViewModel = new ProgressBarViewModel();
            ProgressBarViewModel.OnCancelledHandler += (sender, args) =>
                {
                    try
                    {
                        chapterAdder.CancelOperation(string.Empty);
                    }
                    catch (Exception ex)
                    {
                        ShowMessage(new MessageBoxEventArgs(ex.Message, MessageBoxEventArgs.MessageTypeEnum.Error, MessageBoxButton.OK, MessageBoxImage.Error));
                    }
                };
            Task.Run(() => chapterAdder.AddChapters());
            Navigator.Instance.OpenChildWindow.Execute(ProgressBarViewModel);
        }

        private void StartCommandExecute()
        {
            StartTimeSet = true;
            StartTime = GetPlayerPosition(player);
        }

        private void EndCommandExecute()
        {
            if (GetPlayerPosition(player) < StartTime)
            {
                StartTimeSet = false;
                StartTime = TimeSpan.FromMilliseconds(0);
                ShowMessage(new MessageBoxEventArgs(Translatables.EndTimeAfterStartTime, MessageBoxEventArgs.MessageTypeEnum.Error, MessageBoxButton.OK, MessageBoxImage.Error));
                return;
            }
            EndTimeSet = true;
            EndTime = GetPlayerPosition(player);
            AddRectangle();

            if (AddChapters)
                new ChapterTitleDialogView(this) { WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = Application.Current.MainWindow }.ShowDialog();

            ChapterMarkers.Add(new ChapterMarkerViewModel(StartTime, EndTime, NewChapter));
            NewChapter = string.Empty;
            StartTimeSet = EndTimeSet = false;
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

            InputPath = openFileDialog.FileName;
            GetDetails(player, openFileDialog.FileName);
            player.Open(new Uri(openFileDialog.FileName));
            player.UpdateLayout();
            FileLoaded = true;
            ResetAll();

            var args = new MessageBoxEventArgs(Translatables.AddChaptersMessage, MessageBoxEventArgs.MessageTypeEnum.Question, MessageBoxButton.YesNo, MessageBoxImage.Question);
            ShowMessage(args);
            var chaptersCompatible = canAddChapters();
            if (args.Result == MessageBoxResult.Yes && !chaptersCompatible)
                ShowMessage(new MessageBoxEventArgs($"Chapter markers are only compatible with the following formats: {string.Join(", ", FormatTypeViewModel.ChapterMarkerCompatibleFormats.Select(f => f.ToString()))}", MessageBoxEventArgs.MessageTypeEnum.Information, MessageBoxButton.OK, MessageBoxImage.Information));//todo
            AddChapters = args.Result == MessageBoxResult.Yes && chaptersCompatible;

            bool canAddChapters() => FormatTypeViewModel.ChapterMarkerCompatibleFormats.Contains((FormatEnum)Enum.Parse(typeof(FormatEnum), Path.GetExtension(openFileDialog.FileName).Substring(1)));
        }

        private void ImportCommandExecute()
        {
            ShowMessage(new MessageBoxEventArgs($"{Translatables.ChapterFileFormatMessage}\nStartTime,Title\n00:00:00,Chapter 1\n00:30:14,Chapter 2", MessageBoxEventArgs.MessageTypeEnum.Information, MessageBoxButton.OK, MessageBoxImage.Information));
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

        private void AddRectangle()
        {
            Application.Current.Dispatcher.Invoke(() => RectCollection.Add(new Rectangle()));
            var rect = RectCollection[RectCollection.Count - 1];
            rect.MouseDown += Rect_MouseDown;
            rect.Margin = new Thickness(mapToRange(StartTime.TotalMilliseconds, 760, slider.Maximum), 0, 0, 0);
            rect.Width = mapToRange((EndTime - StartTime).TotalMilliseconds, 760, slider.Maximum);
            rect.Height = 5;
            rect.HorizontalAlignment = HorizontalAlignment.Left;
            rect.Fill = new SolidColorBrush(Colors.Red);

            double mapToRange(double toConvert, double maxRange1, double maxRange2) => toConvert * (maxRange1 / maxRange2);
        }

        private void ClearAllRectangles()
        {
            RectCollection.Clear();
            ChapterMarkers.Clear();
        }

        private void ResetAll()
        {
            StartTimeSet = EndTimeSet = false;
            StartTime = EndTime = TimeSpan.FromMilliseconds(0);
            CurrentTimeString = "00:00:00:000";
            TimesImported = false;
            importedFile = string.Empty;
            ClearAllRectangles();
        }

        private void Splitter_StartedDownload(object sender, DownloadStartedEventArgs e) => ProgressBarViewModel.UpdateLabel(e.Label);

        private void Splitter_ProgressDownload(object sender, ProgressEventArgs e)
        {
            if (e.Percentage > ProgressBarViewModel.ProgressBarCollection[e.ProcessIndex].ProgressValue)
                ProgressBarViewModel.UpdateProgressValue(e.Percentage);
        }

        private void Splitter_FinishedDownload(object sender, FinishedEventArgs e)
        {
            var chaptersAdded = AddChapters;
            CleanUp();
            ClosePlayer(player);
            var message = e.Cancelled
                ? $"{Translatables.OperationCancelled} {e.Message}"
                : chaptersAdded ? Translatables.ChaptersSuccessfullyAdded : Translatables.VideoSuccessfullySplit;
            ShowMessage(new MessageBoxEventArgs(message, MessageBoxEventArgs.MessageTypeEnum.Information, MessageBoxButton.OK, MessageBoxImage.Information));
        }

        private void Splitter_ErrorDownload(object sender, ProgressEventArgs e)
        {
            CleanUp();
            ShowMessage(new MessageBoxEventArgs($"{Translatables.ErrorOccurred}\n\n{e.Error}", MessageBoxEventArgs.MessageTypeEnum.Error, MessageBoxButton.OK, MessageBoxImage.Error));
        }

        private void CleanUp()
        {
            Navigator.Instance.CloseChildWindow.Execute(false);
            StartTime = EndTime = TimeSpan.FromMilliseconds(0);
            CombineVideo = false;
            AddChapters = false;
            OutputDifferentFormat = false;
            ReEncodeVideo = false;
            FileLoaded = false;
            TimesImported = false;
            importedFile = string.Empty;
            FormatType = FormatEnum.avi;
            ClearAllRectangles();
        }
    }

    public class ChapterMarkerViewModel : ViewModel
    {
        private TimeSpan startTime;
        private TimeSpan endTime;
        private string title;

        public TimeSpan StartTime
        {
            get => startTime;
            set => SetProperty(ref startTime, value);
        }

        public TimeSpan EndTime
        {
            get => endTime;
            set => SetProperty(ref endTime, value);
        }
        public string Title
        {
            get => title;
            set => SetProperty(ref title, value);
        }


        public ChapterMarkerViewModel(TimeSpan start, TimeSpan end, string title)
        {
            StartTime = start;
            EndTime = end;
            Title = title;
        }
    }
}
