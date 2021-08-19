using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CSMediaProperties;

namespace VideoEditorUi.Utilities
{
    public static class UtilityClass
    {
        public static void InitializePlayer(CSVideoPlayer.VideoPlayerWPF player)
        {
            var libsPath = "";
            var directoryName = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            if (directoryName != null)
                libsPath = Path.Combine(directoryName, "Binaries", "CSPlugins", "FFmpeg", IntPtr.Size == 8 ? "x64" : "x86");
            player.Init(libsPath, "UserName", "RegKey");
        }

        public static async void GetDetails(CSVideoPlayer.VideoPlayerWPF player, string name) => player.mediaProperties = await player.GetDeatils(name);

        /// <summary>
        /// Handles MPEG-TS files since the Start time differs from other files
        /// </summary>
        /// <returns></returns>
        public static TimeSpan GetPlayerPosition(CSVideoPlayer.VideoPlayerWPF player) => player.PositionGet() - TimeSpan.FromSeconds(double.Parse(player.mediaProperties.Format.StartTime));

        public static void SetPlayerPosition(CSVideoPlayer.VideoPlayerWPF player, double newValue)
        {
            player.PositionSet(new TimeSpan(0, 0, 0, 0, (int)newValue) + TimeSpan.FromSeconds(double.Parse(player.mediaProperties.Format.StartTime)));
        }
    }
}
