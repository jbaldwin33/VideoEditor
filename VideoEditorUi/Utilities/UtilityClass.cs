using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CSMediaProperties;
using CSVideoPlayer;

namespace VideoEditorUi.Utilities
{
    public static class UtilityClass
    {
        public static void InitializePlayer(VideoPlayerWPF player)
        {
            var binaryPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Binaries");
            if (string.IsNullOrEmpty(binaryPath))
                throw new Exception("Cannot read 'binaryFolder' variable from app.config / web.config.");

            player.Init(binaryPath, "UserName", "RegKey");
        }

        public static async void GetDetails(VideoPlayerWPF player, string name) => player.mediaProperties = await player.GetDeatils(name);

        /// <summary>
        /// Handles MPEG-TS files since the Start time differs from other files
        /// </summary>
        /// <returns></returns>
        public static TimeSpan GetPlayerPosition(VideoPlayerWPF player) => player.PositionGet() - TimeSpan.FromSeconds(double.Parse(player.mediaProperties.Format.StartTime));

        public static void SetPlayerPosition(VideoPlayerWPF player, double newValue)
        {
            player.PositionSet(new TimeSpan(0, 0, 0, 0, (int)newValue) + TimeSpan.FromSeconds(double.Parse(player.mediaProperties.Format.StartTime)));
        }

        public static void ClosePlayer(VideoPlayerWPF player)
        {
            var mediaPlayer = player.GetType().GetField("mediaPlayer", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(player);
            mediaPlayer?.GetType().GetMethod("Close")?.Invoke(mediaPlayer, null);
        }
    }
}
