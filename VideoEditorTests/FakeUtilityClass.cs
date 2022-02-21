using CSVideoPlayer;
using MVVMFramework.ViewModels;
using System;
using System.Threading.Tasks;
using VideoEditorUi.Services;
using VideoEditorUi.Utilities;
using VideoEditorUi.ViewModels;
using VideoUtilities;

namespace VideoEditorTests
{
    public class FakeUtilityClass : IUtilityClass
    {
        private TimeSpan timeSpan = new TimeSpan();

        public void CloseChildWindow(bool isError)
        {
            
        }

        public void ClosePlayer(VideoPlayerWPF player) { }
        public void GetDetails(VideoPlayerWPF player, string name) { }
        public TimeSpan GetPlayerPosition(VideoPlayerWPF player)
        {
            timeSpan = timeSpan.Add(new TimeSpan(0, 0, 5));
            return timeSpan;
        }
        public void InitializePlayer(VideoPlayerWPF player) { }

        public void OpenChildWindow(ViewModel viewModel)
        {
            
        }

        public void SetPlayerPosition(VideoPlayerWPF player, double newValue) { }
    }

    public class FakeEditorService : IVideoEditorService
    {
        public void CancelOperation(string message)
        {
        }

        public void DoPreWork()
        {
        }

        public void DoSetup()
        {
        }

        public void ExecuteVideoEditor(EditorViewModel.StageEnum stage, string label)
        {
        }

        public bool IsInitialized() => true;

        public void SetEditor(BaseClass editor)
        {
        }

        public void SetInitialized(bool initialized)
        {
        }

        public void SetupEditor(BaseClass.StartedDownloadEventHandler startedDownload, BaseClass.ProgressEventHandler progressDownload, BaseClass.FinishedDownloadEventHandler finishedDownload, BaseClass.ErrorEventHandler errorDownload, BaseClass.MessageEventHandler libraryMessageHandler, BaseClass.UpdatePlaylistEventHandler updatePlaylist, BaseClass.PreWorkFinishedEventHandler preWorkFinished, BaseClass.FirstWorkFinishedEventHandler firstWorkFinished)
        {
        }
    }
}
