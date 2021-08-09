using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using VideoUtilities;

namespace VideoEditorUi.ViewModels
{
    public class SplitterViewModel : ViewModelBase
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
        private bool combineVideo;

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

        public bool CombineVideo
        {
            get => combineVideo;
            set => Set(ref combineVideo, value);
        }

        public VideoSplitter Splitter { get; set; }

        public Action<int> RectRemoved;
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

        public event EventHandler<AddRectEventArgs> AddRectAndSetEventHandler;
        public event EventHandler ClearAllRectsEventHandler;

        public void AddRectAndSetEvent() => AddRectAndSetEventHandler?.Invoke(this, new AddRectEventArgs(StartTime, EndTime));
        public void ClearAllRectsEvent() => ClearAllRectsEventHandler?.Invoke(this, new EventArgs());

        public SplitterViewModel()
        {
            Player = player;
            Slider = slider;
            StartTime = EndTime = TimeSpan.FromMilliseconds(0);
            CurrentTimeString = "00:00:00:000";
            RectRemoved = (index) => RemoveTime(index);
            PositionChanged = (time) => UpdateCurrentTime(time);
            Times = new ObservableCollection<(TimeSpan, TimeSpan)>();
            Times.CollectionChanged += Times_CollectionChanged;
        }

        private void Times_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Reset)
                    SplitCommand.RaiseCanExecuteChanged();
            });

        }

        private bool PlayCommandCanExecute() => true;
        private bool PauseCommandCanExecute() => true;
        private bool StopCommandCanExecute() => true;
        private bool StartCommandCanExecute() => !StartTimeSet && FileLoaded;
        private bool EndCommandCanExecute() => StartTimeSet && !EndTimeSet;
        private bool SplitCommandCanExecute() => Times.Count > 0;
        private bool SelectFileCommandCanExecute() => true;

        private void PlayCommandExecute() => player.Play();

        private void PauseCommandExecute() => player.Pause();

        private void StopCommandExecute() => player.Stop();

        private void SplitCommandExecute()
        {
            Splitter = new VideoUtilities.VideoSplitter(SourceFolder, Filename, Extension, Times, CombineVideo);
            Splitter.StartedDownload += Splitter_StartedDownload;
            Splitter.ProgressDownload += Splitter_ProgressDownload;
            Splitter.FinishedDownload += Splitter_FinishedDownload;
            Splitter.ErrorDownload += Splitter_ErrorDownload;
            Splitter.Download();
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
            Times.Add((StartTime, EndTime));
            AddRectAndSetEvent();
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
                //player.Source = null;
                player.Open(new Uri(openFileDialog.FileName));
                //player.Stop();
                FileLoaded = true;
                ResetAll();
            }
        }

        private void RemoveTime(int index) => Times.RemoveAt(index);

        private void UpdateCurrentTime(TimeSpan time) => CurrentTimeString = time.ToString("hh':'mm':'ss':'fff");

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
            if (e.Percentage > ProgressValue)
                ProgressValue = e.Percentage;
            OutputData = e.Data;
        }

        private void Splitter_FinishedDownload(object sender, DownloadEventArgs e)
        {
            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                StartTime = EndTime = TimeSpan.FromMilliseconds(0);
                CombineVideo = false;
                Times.Clear();
                ClearAllRectsEvent();
                MessageBox.Show("Video successfully split.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                ProgressValue = -1;
            });

        }

        private void Splitter_ErrorDownload(object sender, ProgressEventArgs e)
        {
            MessageBox.Show($"An error has occurred. Please close and reopen the program. Check your task manager and make sure any remaining \"ffmpeg.exe\" tasks are ended.\n\n{e.Error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public class AddRectEventArgs : EventArgs
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public AddRectEventArgs(TimeSpan t1, TimeSpan t2)
        {
            StartTime = t1;
            EndTime = t2;
        }
    }
}
