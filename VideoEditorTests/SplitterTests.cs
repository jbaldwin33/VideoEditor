using FizzWare.NBuilder;
using Moq;
using Moq.Protected;
using VideoEditorUi.Utilities;
using VideoEditorUi.ViewModels;
using Xunit;

namespace VideoEditorTests
{
    
    public class SplitterTests
    {
        [Fact]
        public void Should_Clear_Collection_After_Finish()
        {
            var sut = SetupClass.CreateSut<SplitterViewModel>();
            
            sut.StartCommand.Execute(null);
            sut.EndCommand.Execute(null);
            sut.SplitCommand.Execute(null);
        }
    }
}