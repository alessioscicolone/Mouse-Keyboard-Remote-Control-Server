using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Application = System.Windows.Application;

namespace Server
{
    public partial class MainWindow
    {

        private System.Windows.Forms.NotifyIcon _trayIcon;
        private MyServer ms;
        public MainWindow()
        {
            InitializeComponent();
            startImage.Source = Imaging.CreateBitmapSourceFromHBitmap(Server.Properties.Resources.start2.GetHbitmap(),
                                   IntPtr.Zero,
                                   Int32Rect.Empty,
                                   BitmapSizeOptions.FromEmptyOptions());
            _trayIcon = new System.Windows.Forms.NotifyIcon();
            setStopIcon();
            _trayIcon.Visible = true;
            _trayIcon.DoubleClick += delegate (object sender, EventArgs args)
            {
                this.Show();
                this.WindowState = WindowState.Normal;
            };
            _trayIcon.Click += delegate (object sender, EventArgs args)
            {
                this.Hide();
            };
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (this.Port.Text.All(Char.IsDigit) && Int32.Parse(Port.Text) <= 65535 && !String.IsNullOrEmpty(Port.Text) && !String.IsNullOrEmpty(Username.Text) && !String.IsNullOrEmpty(Password.Password))
            {
                if (labelstart.Text.Equals("Start"))
                {
                    this.Hide();
                    try
                    {
                        ms = new MyServer(Int32.Parse(Port.Text), Username.Text, Password.Password, this);
                    }
                    catch (SocketException se)
                    {
                        PortAlreadyInUse();
                        return;
                    }
                    setPauseIcon();
                    labelstart.Text = "Stop";
                    Port.IsReadOnly = true;
                    Username.IsReadOnly = true;
                    Password.IsEnabled = false;
                    _trayIcon.ShowBalloonTip(500, "Controllo Remoto", "The server now accepts connections", ToolTipIcon.Info);
                }
                else
                {
                    ms.stop();
                    labelstart.Text = "Start";
                    setStopIcon();
                    Port.IsReadOnly = false;
                    Username.IsReadOnly = false;
                    Password.IsEnabled = true;
                    _trayIcon.ShowBalloonTip(500, "Controllo Remoto", "The server is now stopped", ToolTipIcon.Info);

                }

            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Assicurati di aver inserito tutti i campi", "Errore", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                labelstart.Text = "Start";
                Port.IsReadOnly = false;

            }
        }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

            if (ms != null)
                ms.stop();

            _trayIcon.Visible = false;
            Application.Current.Shutdown();

        }

        public void ConnectionClosed()
        {

            if (ms != null && !ms.WasStopped())
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    resetIpWindow(true);
                    setPlayIcon();
                    _trayIcon.ShowBalloonTip(500, "Controllo Remoto", "Connection Problems, accepting new connection!", ToolTipIcon.Info);
                }));
            }

        }

        public void ConnectionProblem()
        {
            if (ms != null)
                ms.stop();

            Dispatcher.Invoke(new Action(() =>
            {

                setStopIcon();
                start.Content = "Start";
                Port.IsReadOnly = false;
                Username.IsReadOnly = false;
                Password.IsEnabled = true;
                _trayIcon.ShowBalloonTip(500, "Controllo Remoto", "Error: Connection Problems", ToolTipIcon.Info);
            }));
        }

        public void PortAlreadyInUse()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                System.Windows.Forms.MessageBox.Show("La porta scelta è già in uso", "Errore", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                labelstart.Text = "Start";
                setStopIcon();
                Port.IsReadOnly = false;
                Username.IsReadOnly = false;
                Password.IsEnabled = true;
                _trayIcon.ShowBalloonTip(500, "Controllo Remoto", "The server is now stopped", ToolTipIcon.Info);
            }));
        }

        public void resetIpWindow(Boolean onlyRemote)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                if (onlyRemote)
                    remoteip.Text = "Remote Ip Address: ";

                else
                {
                    remoteip.Text = "Remote Ip Address: ";
                    localip.Text = "Local Ip Address: ";

                }
            }));

        }

        public void writeIpWindow(String remote, String local)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                if (remote != null)
                    remoteip.Text += remote;

                if (local != null)
                    localip.Text += local;

            }));
        }

        public void setRecIcon()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                _trayIcon.Icon = Server.Properties.Resources.rec4;
            }));
        }

        public void setPlayIcon()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                _trayIcon.Icon = Server.Properties.Resources.play1normal;
            }));
        }

        public void setStopIcon()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                _trayIcon.Icon = Server.Properties.Resources.stop2;
            }));
        }

        public void setPauseIcon()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                _trayIcon.Icon = Server.Properties.Resources.pauseicon;
            }));
        }

        private void Expander_Collapsed(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.Height = Application.Current.MainWindow.Height - 70;

        }

        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.Height = Application.Current.MainWindow.Height + 70;

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow = this;
        }

    }
}









