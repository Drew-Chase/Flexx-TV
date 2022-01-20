using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;
using Path = System.IO.Path;

namespace Flexx.Binary.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Private Fields

        private ContextMenuStrip _contextMenu;

        private NotifyIcon _notifyIcon;

        private Process _process;

        private bool _started = false;

        #endregion Private Fields

        #region Public Constructors

        public MainWindow()
        {
            InitializeComponent();
            SystemTray();
            StartProcess();
        }

        #endregion Public Constructors

        #region Private Methods

        private void StartProcess()
        {
            string ExecutingBinary = Path.Combine(Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName, "net6.0", "fms_console.exe");

            _process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = ExecutingBinary,
                    Arguments = "-hidden",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                },
                EnableRaisingEvents = true,
            };

            _process.Start();
        }

        private void SystemTray()
        {
            Hide();
            _notifyIcon = new NotifyIcon
            {
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location)
            };
            _notifyIcon.Visible = true;

            _contextMenu = new ContextMenuStrip();
            _contextMenu.Items.Add("Open", null, new EventHandler((object sender, EventArgs args) =>
            {
                Process process = new Process()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = "http://127.0.0.1:3208",
                        Verb = "open"
                    }
                };
            }));

            _contextMenu.Items.Add("Exit", null, new EventHandler((object sender, EventArgs args) =>
            {
                _notifyIcon.Visible = false;
                if (!_process.HasExited)
                {
                    _process.Kill();
                }
                Environment.Exit(0);
            }));
            _notifyIcon.ContextMenuStrip = _contextMenu;
        }

        #endregion Private Methods
    }
}