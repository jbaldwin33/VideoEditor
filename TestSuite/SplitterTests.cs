using System;
using System.Collections.ObjectModel;
using VideoEditorUi.ViewModels;
using VideoUtilities;
using Xunit;

namespace TestSuite
{

    public class SplitterTests
    {
        
        [Theory]
        [InlineData(false, 0)]
        [InlineData(true, 2)]
        public void Verify_Collection_After_Finish(bool isError, int collectionCount)
        {
            var sut = new SplitterViewModel(new FakeUtilityClass(), new FakeEditorService());
            SetupClass.SplitterSetup(sut);
            sut.StartCommand.Execute(null);
            sut.EndCommand.Execute(null);
            sut.StartCommand.Execute(null);
            sut.EndCommand.Execute(null);
            sut.SplitCommand.Execute(null);
            sut.CleanUp(isError);

            Assert.Equal(collectionCount, sut.SectionViewModels.Count);
            Assert.Equal(collectionCount,  sut.RectCollection.Count);
        }

        [Fact]
        public void Verify_Format_On_Load()
        {
            var sut = new SplitterViewModel(new FakeUtilityClass(), new FakeEditorService());
            SetupClass.SplitterSetup(sut);
        }
    }
}