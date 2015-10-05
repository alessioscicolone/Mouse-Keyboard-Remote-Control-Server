using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
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
        //public static PrincipalContext pc = new PrincipalContext(ContextType.Machine, null);
        public MainWindow()
        {
            InitializeComponent();
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
            if (this.Port.Text.All(Char.IsDigit) && !String.IsNullOrEmpty(Port.Text) && !String.IsNullOrEmpty(Username.Text) && !String.IsNullOrEmpty(Password.Password))
            {
                if (labelstart.Text.Equals("Start"))
                {
                    this.Hide();             
                    ms = new MyServer(Int32.Parse(Port.Text), Username.Text, Password.Password);
                    ms.Window = this;
                    setPauseIcon();
                    labelstart.Text = "Stop";
                    Port.IsReadOnly = true;
                    Username.IsReadOnly = true;
                    Password.IsEnabled = false;
                    _trayIcon.ShowBalloonTip(500, "Controllo Remoto", "The server now accepts connections", ToolTipIcon.Info);
                }
                else {
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
            
            if(ms != null)
                ms.stop();

                _trayIcon.Visible = false;
                Application.Current.Shutdown();
            
        }

        public void ConnectionClosed()
        {
           
            if(ms != null && !ms.WasStopped())
            {
                resetIpWindow(true);
                setPauseIcon();
                _trayIcon.ShowBalloonTip(500, "Controllo Remoto", "Connection Problems, accepting new connection!", ToolTipIcon.Info);

            }

        }

        public void ConnectionProblem()
        {
            if (ms != null)
                ms.stop();

            setStopIcon();
            start.Content = "Start";
            Port.IsReadOnly = false;
            Username.IsReadOnly = false;
            Password.IsEnabled = true;
            _trayIcon.ShowBalloonTip(500, "Controllo Remoto", "Error: Connection Problems", ToolTipIcon.Info);

        }

        public void resetIpWindow(Boolean onlyRemote)
        {
            if (onlyRemote)
                remoteip.Text = "Remote Ip Address: ";

            else {
                remoteip.Text = "Remote Ip Address: ";
                localip.Text = "Local Ip Address: ";

            }

        }

        public void writeIpWindow(String remote, String local)
        {
            if(remote != null)
            remoteip.Text += remote;

            if(local != null)
            localip.Text += local;

        }

        public void setPlayIcon()
        {
            _trayIcon.Icon = new System.Drawing.Icon("Resources/rec4.ico");
        }

        public void setPauseIcon()
        {
            _trayIcon.Icon = new System.Drawing.Icon("Resources/play1normal.ico");
        }

        public void setStopIcon()
        {
            _trayIcon.Icon = new System.Drawing.Icon("Resources/stop2.ico");
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



        

       

       
   
