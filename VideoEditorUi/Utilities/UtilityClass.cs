using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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
    }
}
