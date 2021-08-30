using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Serialization;
using CSMediaProperties;
using CSVideoPlayer;

namespace VideoEditorUi.Utilities
{
    public static class UtilityClass
    {
        public static string GetBinaryPath()
        {
            var binaryPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Binaries");
            if (string.IsNullOrEmpty(binaryPath))
                throw new Exception("Cannot read 'binaryFolder' variable from app.config / web.config.");
            return binaryPath;
        }
        public static void InitializePlayer(VideoPlayerWPF player) => player.Init(GetBinaryPath(), "UserName", "RegKey");

        public static async void GetDetails(VideoPlayerWPF player, string name) => player.mediaProperties = await GetVideoDetails(name);

        /// <summary>
        /// Handles MPEG-TS files since the Start time differs from other files
        /// </summary>
        /// <returns></returns>
        public static TimeSpan GetPlayerPosition(VideoPlayerWPF player) => player.PositionGet() - TimeSpan.FromSeconds(double.Parse(player.mediaProperties.Format.StartTime));

        public static void SetPlayerPosition(VideoPlayerWPF player, double newValue)
            => player.PositionSet(new TimeSpan(0, 0, 0, 0, (int)newValue) + TimeSpan.FromSeconds(double.Parse(player.mediaProperties.Format.StartTime)));

        public static void ClosePlayer(VideoPlayerWPF player)
        {
            var mediaPlayer = player.GetType().GetField("mediaPlayer", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(player);
            mediaPlayer?.GetType().GetMethod("Close")?.Invoke(mediaPlayer, null);
        }

        private static async Task<MediaProperties> GetVideoDetails(string input)
        {
            var output = await DoProcess(input);
            var serializer = new XmlSerializer(typeof(MediaProperties));
            MediaProperties mediaProperties;
            using (var reader = new StringReader(output))
                mediaProperties = (MediaProperties)serializer.Deserialize(reader);
            return mediaProperties;
        }

        private static async Task<string> DoProcess(string input)
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
                    output = await reader.ReadToEndAsync();
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
