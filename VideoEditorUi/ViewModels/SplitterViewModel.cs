using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
using VideoUtilities;
using static VideoUtilities.Enums.Enums;
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
        private RelayCommand selectFileCommand;
        private VideoPlayerWPF player;
        private Slider slider;
        private TimeSpan startTime;
        private TimeSpan endTime;
        private string startTimeString;
        private string currentTimeString;
        private decimal progressValue = -1;
        private bool startTimeSet;
        private bool endTimeSet;
        private bool fileLoaded;
        private bool reEncodeVideo;
        private string sourceFolder;
        private string filename;
        private string extension;
        private ObservableCollection<(TimeSpan, TimeSpan)> times;
        private ObservableCollection<Rectangle> rectCollection;
        private ObservableCollection<FormatTypeViewModel> formats;
        private FormatEnum formatType;
        private bool canCombine;
        private bool combineVideo;
        private bool outputDifferentFormat;
        private ProgressBarViewModel progressBarViewModel;

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

        public decimal ProgressValue
        {
            get => progressValue;
            set => SetProperty(ref progressValue, value);
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

        public string SourceFolder
        {
            get => sourceFolder;
            set => SetProperty(ref sourceFolder, value);
        }


        public string Filename
        {
            get => filename;
            set => SetProperty(ref filename, value);
        }

        public string Extension
        {
            get => extension;
            set => SetProperty(ref extension, value);
        }

        public ObservableCollection<(TimeSpan, TimeSpan)> Times
        {
            get => times;
            set => SetProperty(ref times, value);
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

        public ObservableCollection<FormatTypeViewModel> Formats
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
        public string OutputFormatLabel => Translatables.OutputFormatLabel;
        public string ReEncodeQuestionLabel => Translatables.ReEncodeQuestion;
        public string ReEncodeComment => Translatables.ReEncodeComment;
        #endregion
        public RelayCommand SeekBackCommand => seekBackCommand ?? (seekBackCommand = new RelayCommand(SeekBackCommandExecute, () => FileLoaded));
        public RelayCommand SeekForwardCommand => seekForwardCommand ?? (seekForwardCommand = new RelayCommand(SeekForwardCommandExecute, () => FileLoaded));
        public RelayCommand PlayCommand => playCommand ?? (playCommand = new RelayCommand(PlayCommandExecute, () => true));
        public RelayCommand PauseCommand => pauseCommand ?? (pauseCommand = new RelayCommand(PauseCommandExecute, () => true));
        public RelayCommand StopCommand => stopCommand ?? (stopCommand = new RelayCommand(StopCommandExecute, () => true));
        public RelayCommand StartCommand => startCommand ?? (startCommand = new RelayCommand(StartCommandExecute, () => !StartTimeSet && FileLoaded));
        public RelayCommand EndCommand => endCommand ?? (endCommand = new RelayCommand(EndCommandExecute, () => StartTimeSet && !EndTimeSet));
        public RelayCommand SplitCommand => splitCommand ?? (splitCommand = new RelayCommand(SplitCommandExecute, () => Times.Count > 0));
        public RelayCommand SelectFileCommand => selectFileCommand ?? (selectFileCommand = new RelayCommand(SelectFileCommandExecute, () => true));

        private VideoSplitter splitter;
        private static readonly object _lock = new object();
        //public Action CreateNewPlayer;

        public SplitterViewModel()
        {
            CanCombine = false;
            StartTime = EndTime = TimeSpan.FromMilliseconds(0);
            CurrentTimeString = "00:00:00:000";
            PositionChanged = time => CurrentTimeString = time.ToString("hh':'mm':'ss':'fff");
            Times = new ObservableCollection<(TimeSpan, TimeSpan)>();
            RectCollection = new ObservableCollection<Rectangle>();
            Formats = FormatTypeViewModel.CreateViewModels();
            FormatType = FormatEnum.avi;
            Times.CollectionChanged += Times_CollectionChanged;

            BindingOperations.EnableCollectionSynchronization(RectCollection, _lock);
            BindingOperations.EnableCollectionSynchronization(Times, _lock);
        }

        public override void OnUnloaded()
        {
            Times.CollectionChanged -= Times_CollectionChanged;
            base.OnUnloaded();
        }

        private void Times_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => CanCombine = Times.Count > 1;

        private void Rect_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var args = new MessageBoxEventArgs(Translatables.DeleteSectionConfirm, MessageBoxEventArgs.MessageTypeEnum.Information, MessageBoxButton.YesNo, MessageBoxImage.Question);
            ShowMessage(args);
            if (args.Result == MessageBoxResult.Yes)
            {
                var rect = sender as Rectangle;
                var index = RectCollection.IndexOf(rect);
                RectCollection.Remove(rect);
                Times.RemoveAt(index);
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
            splitter = new VideoSplitter(SourceFolder, Filename, Extension, Times, CombineVideo, OutputDifferentFormat, $".{FormatType}", ReEncodeVideo);
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
            Times.Add((StartTime, EndTime));

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

            if (openFileDialog.ShowDialog() == true)
            {
                SourceFolder = Path.GetDirectoryName(openFileDialog.FileName);
                Filename = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                Extension = Path.GetExtension(openFileDialog.FileName);
                //CreateNewPlayer();
                GetDetails(player, openFileDialog.FileName);
                //Thread.Sleep(100);
                player.Open(new Uri(openFileDialog.FileName));
                FileLoaded = true;
                ResetAll();
            }
        }

        private void AddRectangle()
        {
            RectCollection.Add(new Rectangle());
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
            Times.Clear();
        }

        private void ResetAll()
        {
            StartTimeSet = EndTimeSet = false;
            StartTime = EndTime = TimeSpan.FromMilliseconds(0);
            Times.Clear();
        }

        private void Splitter_StartedDownload(object sender, DownloadStartedEventArgs e) => ProgressBarViewModel.UpdateLabel(e.Label);

        private void Splitter_ProgressDownload(object sender, ProgressEventArgs e)
        {
            if (e.Percentage > ProgressValue)
                ProgressBarViewModel.UpdateProgressValue(e.Percentage);
        }

        private void Splitter_FinishedDownload(object sender, FinishedEventArgs e)
        {
            CleanUp();
            var message = e.Cancelled
                ? $"{Translatables.OperationCancelled} {e.Message}"
                : Translatables.VideoSuccessfullySplit;
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
            OutputDifferentFormat = false;
            ReEncodeVideo = false;
            FormatType = FormatEnum.avi;
            ClearAllRectangles();
        }
    }
}
