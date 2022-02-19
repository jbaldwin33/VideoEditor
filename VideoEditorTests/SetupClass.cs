using FizzWare.NBuilder;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoEditorUi.Utilities;

namespace VideoEditorTests
{
    public interface IProtectedMembers
    {
        IUtilityClass UtilityClass { get; set; }
    }

    public static class SetupClass
    {
        public static T CreateSut<T>() where T : VideoEditorUi.ViewModels.EditorViewModel
        {
            var sut = new Mock<T>();
            
            //sut.UtilityClass = new FakeUtilityClass();
            return sut.Object;
        }
    }
}
