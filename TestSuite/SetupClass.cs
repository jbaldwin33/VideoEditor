using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoEditorUi.Utilities;
using VideoEditorUi.ViewModels;
using VideoUtilities;

namespace TestSuite
{
    public static class SetupClass
    {
        public static void SplitterSetup(SplitterViewModel sut)
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

        public static void ChapterAdderSetup(ChapterAdderViewModel sut)
        {
            sut.StartTimeSet = false;
            sut.StartTime = sut.EndTime = TimeSpan.FromMilliseconds(0);
            sut.CurrentTimeString = "00:00:00:000";
            sut.PositionChanged = time => sut.CurrentTimeString = time.ToString("hh':'mm':'ss':'fff");
            sut.SectionViewModels = new ObservableCollection<SectionViewModel>();
            sut.RectCollection = new ObservableCollection<RectClass>();
            sut.GetPlayerPosition = () => sut.StartTime.Add(new TimeSpan(0, 0, 5));
        }

        public static void ChapterAdderSetup(SpeedChangerViewModel sut)
        {
            sut.WithSlider = false;
            sut.FlipScale = 1;
        }
    }
}
