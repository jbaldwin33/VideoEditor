using System.Collections.ObjectModel;
using static VideoUtilities.Enums.Enums;

namespace VideoEditorUi.ViewModels
{
    public class FormatTypeViewModel
    {
        public FormatEnum FormType { get; set; }
        public string Name { get; set; }
        public FormatTypeViewModel(FormatEnum f, string n)
        {
            FormType = f;
            Name = n;
        }

        public static ObservableCollection<FormatTypeViewModel> CreateViewModels() =>
            new ObservableCollection<FormatTypeViewModel>
            {
                new FormatTypeViewModel(FormatEnum.avi, ".avi"),
                new FormatTypeViewModel(FormatEnum.m4a, ".m4a"),
                new FormatTypeViewModel(FormatEnum.mkv, ".mkv"),
                new FormatTypeViewModel(FormatEnum.mov, ".mov"),
                new FormatTypeViewModel(FormatEnum.mp4, ".mp4"),
                new FormatTypeViewModel(FormatEnum.mpeg, ".mpeg"),
                new FormatTypeViewModel(FormatEnum.mpg, ".mpg"),
                new FormatTypeViewModel(FormatEnum.ts, ".ts"),
                new FormatTypeViewModel(FormatEnum.wmv, ".wmv"),
            };
    }
}
