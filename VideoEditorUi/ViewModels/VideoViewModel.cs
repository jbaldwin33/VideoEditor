using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CSVideoPlayer;
using MVVMFramework;
using MVVMFramework.Localization;
using MVVMFramework.ViewModels;
using VideoEditorUi.Utilities;
using VideoUtilities;

namespace VideoEditorUi.ViewModels
{
    public abstract class VideoViewModel<T> : ViewModel
    {
        protected List<T> ObjectList;
        protected BaseClass<T> VideoEditor;
        protected ProgressBarViewModel ProgressBarViewModel;
        public VideoPlayerWPF Player;

        protected VideoViewModel(BaseClass<T> videoEditor)
        {
            //ObjectList = list;
        }

        public override void OnUnloaded()
        {
            UtilityClass.ClosePlayer(Player);
            base.OnUnloaded();
        }

        protected void SetupEditor(BaseClass<T> videoEditor)
        {
            VideoEditor = videoEditor;
            VideoEditor.StartedDownload += StartedDownload;
            VideoEditor.ProgressDownload += ProgressDownload;
            VideoEditor.FinishedDownload += FinishedDownload;
            VideoEditor.ErrorDownload += ErrorDownload;
            VideoEditor.MessageHandler += LibraryMessageHandler;
        }

        protected void StartedDownload(object sender, DownloadStartedEventArgs e) => ProgressBarViewModel.UpdateLabel(e.Label);

        protected void ProgressDownload(object sender, ProgressEventArgs e)
        {
            if (e.Percentage > ProgressBarViewModel.ProgressBarCollection[e.ProcessIndex].ProgressValue)
                ProgressBarViewModel.UpdateProgressValue(e.Percentage, e.ProcessIndex);
        }

        protected virtual void FinishedDownload(object sender, FinishedEventArgs e)
        {
            CleanUp();
            if (Player != null)
                UtilityClass.ClosePlayer(Player);
        }

        protected void ErrorDownload(object sender, ProgressEventArgs e)
        {
            CleanUp();
            ShowMessage(new MessageBoxEventArgs($"{new ErrorOccurredTranslatable()}\n\n{e.Error}", MessageBoxEventArgs.MessageTypeEnum.Error, MessageBoxButton.OK, MessageBoxImage.Error));
        }

        protected virtual void LibraryMessageHandler(object sender, MessageEventArgs e)
        {
            var args = new MessageBoxEventArgs(e.Message, MessageBoxEventArgs.MessageTypeEnum.Question, MessageBoxButton.YesNo, MessageBoxImage.Question);
            ShowMessage(args);
            e.Result = args.Result == MessageBoxResult.Yes;
        }

        protected virtual void CleanUp() => throw new NotImplementedException();
    }
}
