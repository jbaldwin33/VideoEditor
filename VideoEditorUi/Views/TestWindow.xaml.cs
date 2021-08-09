using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace VideoEditorUi
{
    /// <summary>
    /// Interaction logic for TestWindow.xaml
    /// </summary>
    public partial class TestWindow : Window
    {
        private static System.Timers.Timer _SliderTimer;
        private string _mediafile = "";

        int _rotate = 0;
        int _ScaleX = 0;
        public TestWindow()
        {
            InitializeComponent();

            timelineSlider.AddHandler(MouseLeftButtonDownEvent, new MouseButtonEventHandler(Slider_MouseLeftButtonDown), true);
            timelineSlider.AddHandler(MouseLeftButtonUpEvent, new MouseButtonEventHandler(Slider_MouseLeftButtonUp), true);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var libsPath = "";//Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Binaries");
            string directoryName = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            if (directoryName != null)
                libsPath = Path.Combine(directoryName, Path.Combine(Path.Combine("CSPlugins", "FFmpeg"), IntPtr.Size == 8 ? "x64" : "x86"));
            player.Init(libsPath, "UserName", "RegKey");
            player.MediaOpened += player_MediaOpened;
            //player.Open(new Uri(@"E:\Downloads\210711 Pokemon no Uchi Atsumaru (Yamasaki Ten).ts", UriKind.Relative));
            //player.Play();

            //Get the position with the _SliderTimer
            _SliderTimer = new System.Timers.Timer(200);
            _SliderTimer.Elapsed += OnTimedEvent;
            _SliderTimer.AutoReset = true;

        }

        private void cmdOpen_Click(object sender, RoutedEventArgs e)
        {
            //Open the local media file to play

            OpenFileDialog openFileDialog = new OpenFileDialog();

            if (openFileDialog.ShowDialog() != true)
            {
                return;
            }

            _mediafile = openFileDialog.FileName;

            player.Open(new Uri(_mediafile));
        }

        private void cmdPlay_Click(object sender, RoutedEventArgs e)
        {
            player.Play();

            _SliderTimer.Enabled = true;
        }

        /// <summary>
        /// Pause.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmdPause_Click(object sender, RoutedEventArgs e)
        {
            player.Pause();
        }

        /// <summary>
        /// UnPause.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmdUnPause_Click(object sender, RoutedEventArgs e)
        {
            player.Play();
        }

        /// <summary>
        /// Stop.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmdStop_Click(object sender, RoutedEventArgs e)
        {
            player.Stop();
            _SliderTimer.Enabled = false;
            timelineSlider.Value = 0;
        }

        /// <summary>
        /// Play on full screen.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmdFullScreen_Click(object sender, RoutedEventArgs e)
        {
            player.FullScreen();
        }

        /// <summary>
        /// Filp.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmdFlip_Click(object sender, RoutedEventArgs e)
        {
            //Flip

            if (_ScaleX < 0)
                _ScaleX = 1;
            else
                _ScaleX = -1;

            player.Flip(_ScaleX);
        }

        /// <summary>
        /// Rotate +90.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmdRotate90_Click(object sender, RoutedEventArgs e)
        {
            //Rotate +90

            _rotate += 90;
            player.Rotate(_rotate);
        }

        /// <summary>
        /// Rotate -90.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmdRotate90Minus_Click(object sender, RoutedEventArgs e)
        {
            //Rotate -90

            _rotate -= 90;
            player.Rotate(_rotate);
        }

        /// <summary>
        /// Speed ratio.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void ChangeMediaSpeedRatio(object sender, RoutedPropertyChangedEventArgs<double> args)
        {
            //Set the SpeedRatio

            player.SpeedRatio((int)speedRatioSlider.Value);
        }

        /// <summary>
        /// Volume.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeMediaVolume(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //Set the MediaVolume

            player.Volume((int)volumeSlider.Value);
        }

        /// <summary>
        /// Window closing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Visibility = Visibility.Hidden;
        }

        #region Events

        /// <summary>
        /// Timer event of the slider.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            //Set the slider

            Dispatcher.BeginInvoke(new Action(() => { timelineSlider.Value = player.PositionGet().TotalMilliseconds; }));
        }

        /// <summary>
        /// Stop move the slider when MouseDown.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Slider_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //Seek to the position when MouseLeftButtonUp

            int SliderValue = (int)timelineSlider.Value;

            TimeSpan ts = new TimeSpan(0, 0, 0, 0, SliderValue);
            player.PositionSet(ts);
            _SliderTimer.Enabled = true;
        }

        private void player_MediaOpened(object sender, CSVideoPlayer.MediaOpenedEventArgs e)
        {
            timelineSlider.Maximum = player.NaturalDuration().TotalMilliseconds;
        }

        private void Slider_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _SliderTimer.Enabled = false;
        }

        /// <summary>
        /// On MediaChanged.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void player_MediaChanged(object sender, CSVideoPlayer.MediaOpenedEventArgs e)
        {
            //On MediaChanged
        }

        /// <summary>
        /// On MediaChanging.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void player_MediaChanging(object sender, CSVideoPlayer.MediaOpeningEventArgs e)
        {
            //On MediaChanging
        }

        /// <summary>
        /// On MediaClosed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void player_MediaClosed(object sender, EventArgs e)
        {
            //On MediaClosed
        }

        /// <summary>
        /// On MediaEnded.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void player_MediaEnded(object sender, EventArgs e)
        {
            //On MediaEnded
        }

        /// <summary>
        /// On MediaFailed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void player_MediaFailed(object sender, CSVideoPlayer.MediaFailedEventArgs e)
        {
            //On MediaFailed
        }

        /// <summary>
        /// On MediaInitializing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void videoPlayer_MediaInitializing(object sender, CSVideoPlayer.MediaInitializingEventArgs e)
        {
            //On MediaInitializing
        }

        /// <summary>
        /// On MediaOpening.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void player_MediaOpening(object sender, CSVideoPlayer.MediaOpeningEventArgs e)
        {
            //On MediaOpening
        }

        /// <summary>
        /// On MediaReady.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void videoPlayer_MediaReady(object sender, EventArgs e)
        {
            //On MediaReady
        }

        /// <summary>
        /// On MediaStateChanged.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void videoPlayer_MediaStateChanged(object sender, CSVideoPlayer.MediaStateChangedEventArgs e)
        {
            //On MediaStateChanged
        }

        #endregion

        /// <summary>
        /// Order a license.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmdOrderLicense_Click(object sender, RoutedEventArgs e)
        {
            /// In order to get your own UserName and RegKey and to distribute this library
            /// with your own projects for commercial or any other purpose, please order a license at:
            /// https://www.microncode.com/developers/cs-video-player/?cmd=order

            System.Diagnostics.Process.Start("https://www.microncode.com/developers/cs-video-player/?cmd=order");
        }
    }
}
