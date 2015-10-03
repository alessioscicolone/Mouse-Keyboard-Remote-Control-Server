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
            setRedIcon();
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
                    //      Window = new MainWindow(Username.Text, Password.Text, Int32.Parse(Port.Text));
                    //      Window.Show();
                    labelstart.Text = "Stop";
                    Port.IsReadOnly = true;
                    Username.IsReadOnly = true;
                    Password.IsEnabled = false;
                    _trayIcon.ShowBalloonTip(500, "Controllo Remoto", "The server now accepts connections", ToolTipIcon.Info);
                }
                else {
                    ms.stop();
                    labelstart.Text = "Start";
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
            setRedIcon();
            if(!ms.WasStopped())
            _trayIcon.ShowBalloonTip(500, "Controllo Remoto", "Connection closed by Client, accepting new connection!", ToolTipIcon.Info);

        }

        public void ConnectionProblem()
        {
            if (ms != null)
                ms.stop();

            setRedIcon();
            start.Content = "Start";
            Port.IsReadOnly = false;
            Username.IsReadOnly = false;
            Password.IsEnabled = true;
            _trayIcon.ShowBalloonTip(500, "Controllo Remoto", "Error: Connection Problems", ToolTipIcon.Info);

        }

        public void setGreenIcon()
        {
            _trayIcon.Icon = new System.Drawing.Icon("Resources/green.ico");
        }

        public void setRedIcon()
        {
            _trayIcon.Icon = new System.Drawing.Icon("Resources/red.ico");
        }

    }
}



        

       

       
   
