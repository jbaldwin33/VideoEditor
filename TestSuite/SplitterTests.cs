using System;
using System.Collections.ObjectModel;
using VideoEditorUi.ViewModels;
using VideoUtilities;
using Xunit;

namespace TestSuite
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
            sut.GetPlayerPosition = () => sut.StartTime.Add(new TimeSpan(0, 0, 5));
            sut.AddRectangleEvent = () => sut.RectCollection.Add(new RectClass());
        }
        [Theory]
        [InlineData(false, 0)]
        [InlineData(true, 2)]
        public void Verify_Collection_After_Finish(bool isError, int collectionCount)
        {
            var sut = new SplitterViewModel(new FakeUtilityClass(), new FakeEditorService());
            Setup(sut);
            sut.StartCommand.Execute(null);
            sut.EndCommand.Execute(null);
            sut.StartCommand.Execute(null);
            sut.EndCommand.Execute(null);
            sut.SplitCommand.Execute(null);
            sut.CleanUp(isError);

            Assert.Equal(collectionCount, sut.SectionViewModels.Count);
            Assert.Equal(collectionCount,  sut.RectCollection.Count);
        }

    }
}