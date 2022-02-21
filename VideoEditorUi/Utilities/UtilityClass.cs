using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Serialization;
using CSMediaProperties;
using CSVideoPlayer;
using MVVMFrameworkNet472.ViewModels;
using MVVMFrameworkNet472.ViewNavigator;

namespace VideoEditorUi.Utilities
{
    public interface IUtilityClass
    {
        void OpenChildWindow(ViewModel viewModel);
        void CloseChildWindow(bool isError);
        void InitializePlayer(VideoPlayerWPF player);
        void GetDetails(VideoPlayerWPF player, string name);
        TimeSpan GetPlayerPosition(VideoPlayerWPF player);
        void SetPlayerPosition(VideoPlayerWPF player, double newValue);
        void ClosePlayer(VideoPlayerWPF player);
    }

    public class UtilityClass : IUtilityClass
    {
        private static readonly Lazy<UtilityClass> lazy = new Lazy<UtilityClass>(() => new UtilityClass());
        public static UtilityClass Instance => lazy.Value;

        public void OpenChildWindow(ViewModel viewModel) => Navigator.Instance.OpenChildWindow.Execute(viewModel);
        public void CloseChildWindow(bool isError) => Navigator.Instance.CloseChildWindow.Execute(isError);

        public string GetBinaryPath()
        {
            var binaryPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Binaries");
            if (string.IsNullOrEmpty(binaryPath))
                throw new Exception("Cannot read 'binaryFolder' variable from app.config / web.config.");
            return binaryPath;
        }
        public void InitializePlayer(VideoPlayerWPF player) => player.Init(GetBinaryPath(), "UserName", "RegKey");

        public void GetDetails(VideoPlayerWPF player, string name) => player.mediaProperties = GetVideoDetails(name);

        /// <summary>
        /// Handles MPEG-TS files since the Start time differs from other files
        /// </summary>
        /// <returns></returns>
        public TimeSpan GetPlayerPosition(VideoPlayerWPF player) => player.PositionGet() - TimeSpan.FromSeconds(double.Parse(player.mediaProperties.Format.StartTime));

        public void SetPlayerPosition(VideoPlayerWPF player, double newValue)
            => player.PositionSet(new TimeSpan(0, 0, 0, 0, (int)newValue) + TimeSpan.FromSeconds(double.Parse(player.mediaProperties.Format.StartTime)));

        public void ClosePlayer(VideoPlayerWPF player) => player.Open(null);

        private MediaProperties GetVideoDetails(string input)
        {
            var output = DoProcess(input);
            var serializer = new XmlSerializer(typeof(MediaProperties));
            MediaProperties mediaProperties;
            using (var reader = new StringReader(output))
                mediaProperties = (MediaProperties)serializer.Deserialize(reader);
            return mediaProperties;
        }

        private string DoProcess(string input)
        {
            string output;
            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = Path.Combine(GetBinaryPath(), "ffprobe.exe"),
                    CreateNoWindow = true,
                    Arguments = $"-v quiet -print_format xml -show_streams -show_format \"{input}\""
                };
                process.Start();
                using (var reader = process.StandardOutput)
                    output = reader.ReadToEnd();
                process.WaitForExit();
            };
            return output;
        }

        [XmlRoot("ffprobe")]
        [ReadOnly(true)]
        public class MyMediaProperties
        {
            [Description("Media Information Streams.")]
            [TypeConverter(typeof(ExpandableObjectConverter))]
            [XmlElement("streams")]
            public Streams Streams { get; set; }

            [Description("Media Information Formats.")]
            [TypeConverter(typeof(ExpandableObjectConverter))]
            [XmlElement("format")]
            public Format Format { get; set; }
        }
    }
}
