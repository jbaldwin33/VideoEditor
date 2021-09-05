using System;
using System.Collections.Generic;
using static VideoUtilities.Enums;

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

        public static List<FormatTypeViewModel> CreateViewModels() =>
            new List<FormatTypeViewModel>
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

        public static FormatEnum[] ChapterMarkerCompatibleFormats => new[]
        {
            FormatEnum.mkv,
            FormatEnum.mov,
            FormatEnum.mp4,
            FormatEnum.wmv
        };

        public static bool IsVideoFile(string extension) => Enum.TryParse(extension, true, out FormatEnum _);
    }
}
