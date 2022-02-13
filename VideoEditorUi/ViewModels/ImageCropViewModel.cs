using CSVideoPlayer;
using Microsoft.Win32;
using MVVMFramework.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoUtilities;
using VideoEditorUi.Utilities;
using MVVMFramework.Localization;
using System.IO;

namespace VideoEditorUi.ViewModels
{
    public class ImageCropViewModel : EditorViewModel
    {
        #region Fields and props

        private string inputPath;
        private string oldSize;
        private string newSize;
        private string position;
        private string aspectRatio;
        //private bool fileLoaded;
        private RelayCommand selectFileCommand;
        private RelayCommand cropCommand;
        //private RelayCommand openCropWindowCommand;
        private bool padHeight;
        private double? movePos;
        private double? width;
        private double? height;

        public string InputPath
        {
            get => inputPath;
            set => SetProperty(ref inputPath, value);
        }

        public string OldSize
        {
            get => oldSize;
            set => SetProperty(ref oldSize, value);
        }

        public string NewSize
        {
            get => newSize;
            set => SetProperty(ref newSize, value);
        }

        public string Position
        {
            get => position;
            set => SetProperty(ref position, value);
        }

        public string AspectRatio
        {
            get => aspectRatio;
            set => SetProperty(ref aspectRatio, value);
        }

        #endregion

        #region Commands
        public RelayCommand SelectFileCommand => selectFileCommand ?? (selectFileCommand = new RelayCommand(SelectFileCommandExecute, () => true));
        public RelayCommand CropCommand => cropCommand ?? (cropCommand = new RelayCommand(CropCommandExecute, () => true));

        #endregion

        #region Labels
        public string DragFileLabel => new DragFileTranslatable();
        public string CropLabel => new CropTranslatable();

        #endregion

        private void SelectFileCommandExecute()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "All Video Files|*.png;*.jpg;*.jpeg;*.PNG;*.JPG;*.JPEG",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
            };

            if (openFileDialog.ShowDialog() == false)
                return;

            InputPath = openFileDialog.FileName;
            FileLoaded = true;

            UtilityClass.GetDetails(Player, InputPath);
            width = double.Parse(Player.mediaProperties.Streams.Stream[0].Width);
            height = double.Parse(Player.mediaProperties.Streams.Stream[0].Height);

            padHeight = width > height;
            movePos = Math.Abs(width.Value - height.Value) / 2;
        }


        private void CropCommandExecute()
        {
            var files = Directory.GetFiles("C:\\Users\\Josh\\VSProjects\\Pokedex\\PkdxDatabase\\pokemonicons");
            var counter = 0;
            foreach (var file in files)
            {
                //var justname = Path.GetFileNameWithoutExtension(file);
                //var newname = justname.Replace("-", "");
                //var newfile = Path.Combine(Path.GetDirectoryName(file), $"{newname}.png");
                //File.Move(file, newfile);

                //convert from gif to png
                //var newName = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file));
                //File.Move(file, $"{newName}.png");

                //delete unformatted
                //if (!file.Contains("_formatted"))
                //{
                //    File.Delete(file);
                //}

                //rmove _formatted from name
                //if (file.Contains("_formatted"))
                //{
                //    var justname = Path.GetFileNameWithoutExtension(file);
                //    var newnew = Path.Combine(Path.GetDirectoryName(file), $"{justname.Remove(justname.IndexOf("_formatted"), 10)}.png");
                //    File.Move(file, newnew);
                //}

                UtilityClass.GetDetails(Player, file);
                width = double.Parse(Player.mediaProperties.Streams.Stream[0].Width);
                height = double.Parse(Player.mediaProperties.Streams.Stream[0].Height);

                if (width == height)
                    continue;

                padHeight = width > height;
                movePos = Math.Abs(width.Value - height.Value) / 2;

                VideoEditor = new ImageCropper(file, padHeight ? null : height, padHeight ? width : null, padHeight ? null : movePos, padHeight ? movePos : null);
                Setup(true);
                Execute(StageEnum.Primary, "Cropping image...");
                counter++;
            }
            ShowMessage("done");
        }

        protected override void Initialize()
        {

        }
    }
}
