using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
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
using VideoEditorNetFramework.ViewModels;
using VideoEditorUi.Singletons;
using VideoUtilities;
using static VideoUtilities.Enums.Enums;
using Path = System.IO.Path;

namespace VideoEditorUi.ViewModels
{
    public class SplitterViewModel : BaseViewModel
    {
        private RelayCommand playCommand;
        private RelayCommand pauseCommand;
        private RelayCommand stopCommand;
        private RelayCommand startCommand;
        private RelayCommand endCommand;
        private RelayCommand splitCommand;
        private RelayCommand selectFileCommand;
        private CSVideoPlayer.VideoPlayerWPF player;
        private Slider slider;
        private TimeSpan startTime;
        private TimeSpan endTime;
        private string startTimeString;
        private string currentTimeString;
        private decimal progressValue = -1;
        private string outputData;
        private bool startTimeSet;
        private bool endTimeSet;
        private bool fileLoaded;
        private string sourceFolder;
        private string filename;
        private string extension;
        private ObservableCollection<(TimeSpan, TimeSpan)> times;
        private ObservableCollection<Rectangle> rectCollection;
        private ObservableCollection<FormatTypeViewModel> formats;
        private FormatEnum formatType;
        private bool combineVideo;
        private bool outputDifferentFormat;

        public Slider Slider
        {
            get => slider;
            set => Set(ref slider, value);
        }

        public CSVideoPlayer.VideoPlayerWPF Player
        {
            get => player;
            set => Set(ref player, value);
        }

        public TimeSpan StartTime
        {
            get => startTime;
            set
            {
                Set(ref startTime, value);
                StartTimeString = StartTime.ToString("hh':'mm':'ss':'fff");
            }
        }

        public TimeSpan EndTime
        {
            get => endTime;
            set => Set(ref endTime, value);
        }

        public string StartTimeString
        {
            get => startTimeString;
            set => Set(ref startTimeString, value);
        }

        public string CurrentTimeString
        {
            get => currentTimeString;
            set => Set(ref currentTimeString, value);
        }

        public decimal ProgressValue
        {
            get => progressValue;
            set => Set(ref progressValue, value);
        }

        public string OutputData
        {
            get => outputData;
            set => Set(ref outputData, value);
        }

        public bool StartTimeSet
        {
            get => startTimeSet;
            set
            {
                Set(ref startTimeSet, value);
                StartCommand.RaiseCanExecuteChanged();
                EndCommand.RaiseCanExecuteChanged();
            }
        }

        public bool EndTimeSet
        {
            get => endTimeSet;
            set
            {
                Set(ref endTimeSet, value);
                StartCommand.RaiseCanExecuteChanged();
                EndCommand.RaiseCanExecuteChanged();
            }
        }

        public bool FileLoaded
        {
            get => fileLoaded;
            set
            {
                Set(ref fileLoaded, value);
                StartCommand.RaiseCanExecuteChanged();
                EndCommand.RaiseCanExecuteChanged();
            }
        }

        public string SourceFolder
        {
            get => sourceFolder;
            set => Set(ref sourceFolder, value);
        }


        public string Filename
        {
            get => filename;
            set => Set(ref filename, value);
        }

        public string Extension
        {
            get => extension;
            set => Set(ref extension, value);
        }

        public ObservableCollection<(TimeSpan, TimeSpan)> Times
        {
            get => times;
            set => Set(ref times, value);
        }


        public ObservableCollection<Rectangle> RectCollection
        {
            get => rectCollection;
            set => Set(ref rectCollection, value);
        }

        public FormatEnum FormatType
        {
            get => formatType;
            set => Set(ref formatType, value);
        }

        public ObservableCollection<FormatTypeViewModel> Formats
        {
            get => formats;
            set => Set(ref formats, value);
        }

        public bool CombineVideo
        {
            get => combineVideo;
            set => Set(ref combineVideo, value);
        }

        public bool OutputDifferentFormat
        {
            get => outputDifferentFormat;
            set => Set(ref outputDifferentFormat, value);
        }

        public Action<TimeSpan> PositionChanged;

        public string PlayLabel => "Play";
        public string PauseLabel => "Pause";
        public string StopLabel => "Stop";
        public string StartLabel => "Start time";
        public string EndLabel => "End time";
        public string SplitLabel => "Split";

        public RelayCommand PlayCommand => playCommand ?? (playCommand = new RelayCommand(PlayCommandExecute, PlayCommandCanExecute));
        public RelayCommand PauseCommand => pauseCommand ?? (pauseCommand = new RelayCommand(PauseCommandExecute, PauseCommandCanExecute));
        public RelayCommand StopCommand => stopCommand ?? (stopCommand = new RelayCommand(StopCommandExecute, StopCommandCanExecute));
        public RelayCommand StartCommand => startCommand ?? (startCommand = new RelayCommand(StartCommandExecute, StartCommandCanExecute));
        public RelayCommand EndCommand => endCommand ?? (endCommand = new RelayCommand(EndCommandExecute, EndCommandCanExecute));
        public RelayCommand SplitCommand => splitCommand ?? (splitCommand = new RelayCommand(SplitCommandExecute, SplitCommandCanExecute));
        public RelayCommand SelectFileCommand => selectFileCommand ?? (selectFileCommand = new RelayCommand(SelectFileCommandExecute, SelectFileCommandCanExecute));

        private VideoSplitter splitter;
        private static object _lock = new object();
        private Window window;

        public SplitterViewModel()
        {
            Player = player;
            Slider = slider;
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

        public void CancelOperation() => splitter.CancelOperation();

        private void Times_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Reset)
                Application.Current.Dispatcher.Invoke(() => SplitCommand.RaiseCanExecuteChanged());
        }

        private void Rect_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (MessageBox.Show("Do you want to delete this section?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                var rect = sender as Rectangle;
                var index = RectCollection.IndexOf(rect);
                RectCollection.Remove(rect);
                Times.RemoveAt(index);
            }
        }

        private bool PlayCommandCanExecute() => true;
        private bool PauseCommandCanExecute() => true;
        private bool StopCommandCanExecute() => true;
        private bool StartCommandCanExecute() => !StartTimeSet && FileLoaded;
        private bool EndCommandCanExecute() => StartTimeSet && !EndTimeSet;
        private bool SplitCommandCanExecute() => Times.Count > 0;
        private bool SelectFileCommandCanExecute() => true;

        private void PlayCommandExecute() => Navigator.Instance.OpenChildWindow.Execute(null);//player.Play();

        private void PauseCommandExecute() => player.Pause();

        private void StopCommandExecute() => player.Stop();
        private void SplitCommandExecute()
        {
            splitter = new VideoSplitter(SourceFolder, Filename, Extension, Times, CombineVideo, OutputDifferentFormat, $".{FormatType}");
            splitter.StartedDownload += Splitter_StartedDownload;
            splitter.ProgressDownload += Splitter_ProgressDownload;
            splitter.FinishedDownload += Splitter_FinishedDownload;
            splitter.ErrorDownload += Splitter_ErrorDownload;
            Task.Run(() => splitter.Split());
            Navigator.Instance.OpenChildWindow.Execute(null);
        }

        private void StartCommandExecute()
        {
            StartTimeSet = true;
            StartTime = player.PositionGet();
        }

        private void EndCommandExecute()
        {
            if (player.PositionGet() < StartTime)
            {
                StartTimeSet = false;
                StartTime = TimeSpan.FromMilliseconds(0);
                MessageBox.Show("End time must be after the Start time. Please select the Start time again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            EndTimeSet = true;
            EndTime = player.PositionGet();
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
            rect.Margin = new Thickness(mapToRange(StartTime.TotalMilliseconds, 780, slider.Maximum), 0, 0, 0);
            rect.Width = mapToRange((EndTime - StartTime).TotalMilliseconds, 780, slider.Maximum);
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

        private void Splitter_StartedDownload(object sender, DownloadEventArgs e)
        {

        }

        private void Splitter_ProgressDownload(object sender, ProgressEventArgs e)
        {
            if (e.Percentage > ProgressValue) (Navigator.Instance.ChildViewModel as ProgressBarViewModel).UpdateProgressValue(e.Percentage);
            //ProgressValue = e.Percentage;
            OutputData = e.Data;
        }

        private void Splitter_FinishedDownload(object sender, DownloadEventArgs e)
        {
            Navigator.Instance.CloseChildWindow.Execute(false);
            StartTime = EndTime = TimeSpan.FromMilliseconds(0);
            CombineVideo = false;
            OutputDifferentFormat = false;
            FormatType = FormatEnum.avi;
            ClearAllRectangles();
            MessageBox.Show("Video successfully split.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            ProgressValue = -1;
        }

        private void Splitter_ErrorDownload(object sender, ProgressEventArgs e)
        {
            MessageBox.Show($"An error has occurred. Please close and reopen the program. Check your task manager and make sure any remaining \"ffmpeg.exe\" tasks are ended.\n\n{e.Error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
