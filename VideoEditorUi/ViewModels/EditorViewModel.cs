using System;
using System.Collections.Generic;
using System.Windows;
using MVVMFramework.Localization;
using MVVMFramework.ViewModels;
using VideoEditorUi.Services;
using VideoEditorUi.Utilities;
using VideoUtilities;
using static VideoUtilities.BaseClass;

namespace VideoEditorUi.ViewModels
{
    public abstract class EditorViewModel : ViewModel
    {
        public enum StageEnum { Pre, Primary, Secondary }
        private bool withSlider = true;
        private double sliderValue;
        private bool fileLoaded;
        private bool isPlaying;

        protected IUtilityClass UtilityClass;
        protected IVideoEditorService EditorService;
        protected ProgressBarViewModel ProgressBarViewModel;
        public double SliderMax;
        public Action<string[]> DragFiles;
        public Action<TimeSpan> PositionChanged;
        public Action<double> SeekEvent;
        public Action PlayEvent;
        public Action PauseEvent;
        public Func<string, CSMediaProperties.MediaProperties> GetDetailsEvent;
        public Action<string> OpenEvent;
        public Action ClosePlayerEvent;
        public Func<TimeSpan> GetPlayerPosition;
        public Action<double> SetPlayerPosition;

        public EditorViewModel(IUtilityClass utilityClass, IVideoEditorService editorService)
        {
            UtilityClass = utilityClass;
            EditorService = editorService;
        }

        public bool WithSlider
        {
            get => withSlider;
            set => SetProperty(ref withSlider, value);
        }

        public double SliderValue
        {
            get => sliderValue;
            set => SetProperty(ref sliderValue, value);
        }

        public bool FileLoaded
        {
            get => fileLoaded;
            set => SetProperty(ref fileLoaded, value);
        }

        public bool IsPlaying
        {
            get => isPlaying;
            set => SetProperty(ref isPlaying, value);
        }

        public override void OnLoaded()
        {
            Initialize();
            DragFiles = DragFilesCallback;
            base.OnLoaded();
        }

        public override void OnUnloaded() => ClosePlayerEvent?.Invoke();

        public void Setup(bool doSetup, bool doPreWork, BaseArgs args, PreWorkFinishedEventHandler preWorkFinished, out bool isError, FirstWorkFinishedEventHandler firstWorkFinished, int count = 1, List<UrlClass> urlCollection = null)
        {
            EditorService.SetEditor(args);
            EditorService.SetupEditor(StartedDownload, ProgressDownload, FinishedDownload, ErrorDownload, LibraryMessageHandler, UpdatePlaylist, preWorkFinished, firstWorkFinished);
            EditorService.DoPreCheck(out bool preCheckError);
            isError = preCheckError;
            if (isError)
                return;

            if (doPreWork)
                EditorService.DoPreWork();

            SetupProgressBarViewModel(count, urlCollection);
            if (doSetup)
                EditorService.DoSetup();
        }

        public void Execute(StageEnum stage, string label)
        {
            EditorService.ExecuteVideoEditor(stage, label);
            UtilityClass.OpenChildWindow(ProgressBarViewModel);
        }

        public virtual void Initialize() => throw new NotImplementedException();

        public virtual void CleanUp(bool isError)
        {
            UtilityClass.CloseChildWindow(false);
            EditorService.SetInitialized(false);
            if (isError)
                return;

            ClosePlayerEvent?.Invoke();
        }

        protected void PlayCommandExecute()
        {
            if (!IsPlaying)
                PlayEvent?.Invoke();
            else
                PauseEvent?.Invoke();
            IsPlaying = !IsPlaying;
        }

        protected void StartedDownload(object sender, DownloadStartedEventArgs e) => ProgressBarViewModel.UpdateLabel(e.Label);

        protected void ProgressDownload(object sender, ProgressEventArgs e)
        {
            if (e.Percentage > ProgressBarViewModel.ProgressBarCollection[e.ProcessIndex].ProgressValue)
                ProgressBarViewModel.UpdateProgressValue(e.Percentage, e.ProcessIndex);
        }

        protected void UpdatePlaylist(object sender, PlaylistEventArgs e) => ProgressBarViewModel.ProgressBarCollection[e.Index].UpdatePlaylist(e.Current, e.Total);

        protected virtual void DragFilesCallback(string[] files) => throw new NotImplementedException();

        protected virtual void FinishedDownload(object sender, FinishedEventArgs e) => CleanUp(false);

        protected virtual void ErrorDownload(object sender, ProgressEventArgs e)
        {
            CleanUp(true);
            ShowMessage(new MessageBoxEventArgs($"{new ErrorOccurredTranslatable()}\n\n{e.Error}", MessageBoxEventArgs.MessageTypeEnum.Error, MessageBoxButton.OK, MessageBoxImage.Error));
        }

        protected virtual void LibraryMessageHandler(object sender, MessageEventArgs e)
        {
            var args = new MessageBoxEventArgs(e.Message, GetMessageTypeEnum(e.MessageType), GetMessageBoxButtons(e.MessageBoxButton), GetMessageBoxImage(e.MessageImageType));
            ShowMessage(args);
            e.Result = args.Result == MessageBoxResult.Yes;
        }

        private MessageBoxEventArgs.MessageTypeEnum GetMessageTypeEnum(MessageEventArgs.MessageTypeEnum type)
        {
            switch (type)
            {
                case MessageEventArgs.MessageTypeEnum.Question:
                    return MessageBoxEventArgs.MessageTypeEnum.Question;
                case MessageEventArgs.MessageTypeEnum.Info:
                    return MessageBoxEventArgs.MessageTypeEnum.Information;
                case MessageEventArgs.MessageTypeEnum.Warning:
                    return MessageBoxEventArgs.MessageTypeEnum.Warning;
                case MessageEventArgs.MessageTypeEnum.Error:
                    return MessageBoxEventArgs.MessageTypeEnum.Error;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "");
            }
        }

        private MessageBoxButton GetMessageBoxButtons(MessageEventArgs.MessageBoxButtonsEnum type)
        {
            switch (type)
            {
                case MessageEventArgs.MessageBoxButtonsEnum.Ok:
                    return MessageBoxButton.OK;
                case MessageEventArgs.MessageBoxButtonsEnum.YesNo:
                    return MessageBoxButton.YesNo;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "");
            }
        }

        private MessageBoxImage GetMessageBoxImage(MessageEventArgs.MessageImageTypeEnum type)
        {
            switch (type)
            {
                case MessageEventArgs.MessageImageTypeEnum.Question:
                    return MessageBoxImage.Question;
                case MessageEventArgs.MessageImageTypeEnum.Info:
                    return MessageBoxImage.Information;
                case MessageEventArgs.MessageImageTypeEnum.Warning:
                    return MessageBoxImage.Warning;
                case MessageEventArgs.MessageImageTypeEnum.Error:
                    return MessageBoxImage.Error;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "");
            }
        }

        private void SetupProgressBarViewModel(int count, List<UrlClass> urls = null)
        {
            ProgressBarViewModel = new ProgressBarViewModel(count, urls);
            ProgressBarViewModel.OnCancelledHandler += (sender, args) =>
            {
                try
                {
                    EditorService.CancelOperation(string.Empty);
                }
                catch (Exception ex)
                {
                    ShowMessage(new MessageBoxEventArgs(ex.Message, MessageBoxEventArgs.MessageTypeEnum.Error, MessageBoxButton.OK, MessageBoxImage.Error));
                }
            };
        }
    }
}
