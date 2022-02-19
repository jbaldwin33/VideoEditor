using FizzWare.NBuilder;
using Moq;
using Moq.Protected;
using System;
using System.Collections.ObjectModel;
using VideoEditorUi.Utilities;
using VideoEditorUi.ViewModels;
using VideoUtilities;
using Xunit;

namespace VideoEditorTests
{
    
    public class SplitterTests
    {
        private void Setup(SplitterViewModel sut)
        {
            sut.CanCombine = false;
            sut.StartTimeSet = false;
            sut.StartTime = sut.EndTime = TimeSpan.FromMilliseconds(0);
            sut.CurrentTimeString = "00:00:00:000";
            sut.PositionChanged = time => sut.CurrentTimeString = time.ToString("hh':'mm':'ss':'fff");
            sut.SectionViewModels = new ObservableCollection<SectionViewModel>();
            sut.RectCollection = new ObservableCollection<RectClass>();
            sut.Formats = FormatTypeViewModel.CreateViewModels();
            sut.UtilityClass = new FakeUtilityClass();
            sut.AddRectangleEvent = () => sut.RectCollection.Add(new RectClass());
            //sut.FormatType = FormatEnum.avi;
            //sut.SectionViewModels.CollectionChanged += Times_CollectionChanged;
        }
        [Fact]
        public void Should_Clear_Collection_After_Finish()
        {
            var mock = new Mock<SplitterViewModel>();
            mock.As<IEditorViewModel>().Setup(x => x.Execute(EditorViewModel.StageEnum.Primary, ""))// "Execute", new object[] { EditorViewModel.StageEnum.Pre, "" })
                .Callback(() => { mock.Object.CleanUp(false); })
                .Verifiable();
            var sut = mock.Object;
            Setup(sut);
            sut.StartCommand.Execute(null);
            sut.EndCommand.Execute(null);
            sut.SplitCommand.Execute(null);
        }
    }
}