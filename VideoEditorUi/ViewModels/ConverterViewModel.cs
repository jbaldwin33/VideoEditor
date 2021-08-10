using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoEditorNetFramework.ViewModels;

namespace VideoEditorUi.ViewModels
{
    public class ConverterViewModel : ViewModelBase
    {
        private ObservableCollection<FormatTypeViewModel> formats;
        private string filename;
        private RelayCommand selectFileCommand;
        private RelayCommand convertCommand;
        private string sourceFolder;

        public string Filename
        {
            get => filename;
            set => Set(ref filename, value);
        }

        private string extension;

        public ObservableCollection<FormatTypeViewModel> Formats
        {
            get => formats;
            set => Set(ref formats, value);
        }

        public RelayCommand SelectFileCommand => selectFileCommand ?? (selectFileCommand = new RelayCommand(SelectFileCommandExecute, () => true));
        public RelayCommand ConvertCommand => convertCommand ?? (convertCommand = new RelayCommand(ConvertCommandExecute, ConvertCommandCanExecute)
        public string SelectFileLabel => "Click to select a file...";
        public string ConvertLabel => "Convert";

        public ConverterViewModel()
        {
            Formats = FormatTypeViewModel.CreateViewModels();
        }

        private void SelectFileCommandExecute()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "All Video Files|*.wmv;*.avi;*.mpg;*.mpeg;*.mp4;*.mov;*.m4a;*.mkv;*.ts;*.WMV;*.AVI;*.MPG;*.MPEG;*.MP4;*.MOV;*.M4A;*.MKV;*.TS",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
            };

            if (openFileDialog.ShowDialog() == true)
            {
                sourceFolder = Path.GetDirectoryName(openFileDialog.FileName);
                Filename = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                extension = Path.GetExtension(openFileDialog.FileName);
            }
        }

        private void ResetAll()
        {
        }
    }
}
