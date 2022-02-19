using CSVideoPlayer;
using System;
using System.Threading.Tasks;
using VideoEditorUi.Utilities;

namespace VideoEditorTests
{
    public class FakeUtilityClass : IUtilityClass
    {
        private TimeSpan timeSpan = new TimeSpan();
        public void ClosePlayer(VideoPlayerWPF player) => _ = 0;
        public void GetDetails(VideoPlayerWPF player, string name) => _ = 0;
        public TimeSpan GetPlayerPosition(VideoPlayerWPF player)
        {
            timeSpan = timeSpan.Add(new TimeSpan(0, 0, 5));
            return timeSpan;
        }
        public void InitializePlayer(VideoPlayerWPF player) => _ = 0;
        public void SetPlayerPosition(VideoPlayerWPF player, double newValue) => _ = 0;
    }
}
