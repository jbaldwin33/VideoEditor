using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Xml.Serialization;
using Panel = System.Windows.Forms.Panel;
using Keys = System.Windows.Forms.Keys;

namespace WPFVideoPlayer
{
    /// <summary>
    /// Interaction logic for WPFVideoPlayer.xaml
    /// </summary>
    public partial class WPFVideoPlayer : UserControl
    {
        public event EventHandler MediaOpened;
        public event EventHandler MediaClosed;

        public string SourcePath { get; private set; }
        public TimeSpan StartTime;
        public TimeSpan CurrentTime;
        public TimeSpan Duration;
        public MyMediaProperties MediaProperties;

        private Process process;
        private Panel panel;
        private bool isPlaying;
        private bool isOpened;

        public WPFVideoPlayer()
        {
            InitializeComponent();
            panel = new Panel
            {
                ClientSize = new System.Drawing.Size(600, 300)
            };
            host.Child = panel;
        }

        public void Open(string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                MessageBox.Show("no video");
                return;
            }
            SourcePath = filename;
            process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = Path.Combine(GetBinaryPath(), "ffplay.exe"),
                    CreateNoWindow = true,
                    Arguments = $"-noborder -x 600 -y 300 -seek_interval 5 \"{SourcePath}\"",
                }
            };
            process.Exited += Process_Exited;
            process.OutputDataReceived += Process_OutputDataReceived;
            process.ErrorDataReceived += Process_ErrorDataReceived;
            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            while (process.MainWindowHandle == IntPtr.Zero)
            {
                Thread.Yield();
            }
            VideoWindowUtility.DoWindowStuff(process, panel);
            //set focus to main window to prevent keyboard input to process
            Application.Current.MainWindow.Activate();
            isOpened = true;
            Play();
        }

        public void Play()
        {
            if (!isOpened)
                return;
            isPlaying = !isPlaying;
            VideoWindowUtility.SetForegroundWindow(process.MainWindowHandle);
            Keyboard.KeyPress(Keys.M);
            Keyboard.KeyPress(Keys.Space);
        }

        public void Stop()
        {
            if (!isOpened)
                return;

            isOpened = false;
            isPlaying = false;
            VideoWindowUtility.SetForegroundWindow(process.MainWindowHandle);
            Keyboard.KeyPress(Keys.Escape);
        }

        public void SeekForward()
        {
            if (!isOpened)
                return;
            VideoWindowUtility.SetForegroundWindow(process.MainWindowHandle);
            Keyboard.KeyPress(Keys.Right);
        }

        public void SeekBackward()
        {
            if (!isOpened)
                return;
            VideoWindowUtility.SetForegroundWindow(process.MainWindowHandle);
            Keyboard.KeyPress(Keys.Left);
        }


        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null || !e.Data.Contains("A-V:"))
                return;

            var time = e.Data.Split(new[] { "A-V" }, StringSplitOptions.None);
            var sub = time[0].Substring(0, time[0].Length - 1);
            if (sub.Contains("nan"))
                return;

            CurrentTime = TimeSpan.FromSeconds(double.Parse(sub));
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {

        }

        private void Process_Exited(object sender, EventArgs e)
        {
            isPlaying = false;
            isOpened = false;
        }

        public string GetBinaryPath()
        {
            var binaryPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Binaries");
            if (string.IsNullOrEmpty(binaryPath))
                throw new Exception("Cannot read 'binaryFolder' variable from app.config / web.config.");
            return binaryPath;
        }

        public void GetDetails()
        {
            MediaProperties = GetVideoDetails();
            StartTime = TimeSpan.FromSeconds(double.Parse(MediaProperties.Format.StartTime));
            Duration = TimeSpan.FromSeconds(double.Parse(MediaProperties.Format.Duration));
        }

        private MyMediaProperties GetVideoDetails()
        {
            var output = DoProcess();
            var serializer = new XmlSerializer(typeof(MyMediaProperties));
            MyMediaProperties mediaProperties;
            using (var reader = new StringReader(output))
                mediaProperties = (MyMediaProperties)serializer.Deserialize(reader);
            return mediaProperties;
        }

        private string DoProcess()
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
                    Arguments = $"-v quiet -print_format xml -show_streams -show_format \"{SourcePath}\""
                };
                process.Start();
                using (var reader = process.StandardOutput)
                    output = reader.ReadToEnd();
                process.WaitForExit();
            };
            return output;
        }

        public void Dispose()
        {
            if (process != null && !process.HasExited)
                process.Kill();
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
