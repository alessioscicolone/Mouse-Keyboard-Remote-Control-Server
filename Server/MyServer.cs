using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows;
using System.Security.Cryptography;

namespace Server
{
    class MyServer
    {
        private Input input;
        private TcpListener myList;
        private TcpClient tclient;
        private UdpClient uclient;
        private IPEndPoint localpt;
        private MyClipBoard mcb;
        public MainWindow Window { get; set; }

        private byte[] publicKey;
        private ECDiffieHellmanCng exch;
        private bool logged = false;
        private string username;
        private string password;
        private Boolean accepting = true;


        public MyServer(int port, string username, string password, MainWindow w)
        {
            try
            {
                exch = new ECDiffieHellmanCng(256);
                exch.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
                exch.HashAlgorithm = CngAlgorithm.Sha256;
                publicKey = exch.PublicKey.ToByteArray();
                this.username = username;
                this.password = password;
                this.Window = w;
                input = new Input();
                mcb = new MyClipBoard();
                localpt = new IPEndPoint(IPAddress.Any, port);
                /* Initializes the Listener */
                myList = new TcpListener(localpt);
                uclient = new UdpClient();
                uclient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                uclient.DontFragment = true;
                uclient.Client.Bind(localpt);
                /* Start Listening at the specified port */
                myList.Start();
                Console.WriteLine("The server is running at port 8001...");
                Console.WriteLine("The local End point is :" + myList.LocalEndpoint);
                Console.WriteLine("Waiting for a connection.....");

                Thread t3 = new Thread(InputProcessing);
                t3.Start();
                Thread t = new Thread(ClipBoardProcessing); //LOGIN AND CLIPBOARD PROCESSING
                t.SetApartmentState(ApartmentState.STA);
                t.Start();

            }
            catch (SocketException se)
            {
                if (se.ErrorCode == 10048)
                {
                    throw se;
                }
                Console.WriteLine(se.Message + " " + se.ErrorCode);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }


        }

        public Boolean WasStopped()
        {
            return !accepting;
        }

        public Boolean checkCredentials(string username, string password)
        {
            if (this.username.Equals(username) && this.password.Equals(password))
                return true;
            else return false;
        }

        public Boolean connectAndLogin()
        {
            try
            {
                byte[] clientPublicKey = new byte[72];
                tclient.GetStream().Read(clientPublicKey, 0, 72);
                byte[] derivedKey =
                exch.DeriveKeyMaterial(CngKey.Import(clientPublicKey, CngKeyBlobFormat.EccPublicBlob));
                tclient.GetStream().Write(publicKey, 0, publicKey.Length);
                StreamReader streamReader = new StreamReader(tclient.GetStream());

                string currentUserName = this.username;

                string receivedUserName = streamReader.ReadLine();

                Console.WriteLine("Received Username " + receivedUserName);
                Aes aes = new AesCryptoServiceProvider();
                aes.Key = derivedKey;
                byte[] bytes = new byte[aes.BlockSize / 8];

                bytes.Initialize();
                System.Buffer.BlockCopy(currentUserName.ToCharArray(), 0, bytes, 0,
                    bytes.Length > currentUserName.Length * sizeof(char)
                        ? currentUserName.Length * sizeof(char)
                        : bytes.Length);
                aes.IV = bytes;
                MemoryStream ms = new MemoryStream(64);
                ICryptoTransform encryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                CryptoStream csEncrypt = new CryptoStream(ms, encryptor,
                    CryptoStreamMode.Write);

                string encpassword = streamReader.ReadLine();
                byte[] buffer = Convert.FromBase64String(encpassword);

                csEncrypt.Write(buffer, 0, buffer.Length);

                csEncrypt.Flush();
                csEncrypt.Close();
                string password = Encoding.UTF8.GetString(ms.ToArray());
                IntPtr th = IntPtr.Zero;

                logged = checkCredentials(receivedUserName, password);

                Console.WriteLine("Pass " + password + "User " + receivedUserName);

                Console.WriteLine("Logged: " + logged);

                byte[] auth = BitConverter.GetBytes(logged);
                tclient.GetStream().Write(auth, 0, sizeof(bool));
                Console.WriteLine("INVIO RISPOSTA "+ logged);
                return logged;
            }
            catch(CryptographicException ce)
            {
                byte[] auth = BitConverter.GetBytes(false);
                if(tclient != null)
                tclient.GetStream().Write(auth, 0, sizeof(bool));
                Console.WriteLine("crype ex: " + ce.Message);
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }

        }

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("Local IP Address Not Found!");
        }

        private bool SocketConnected(Socket s)
        {
            try
            {
                if (s != null)
                {
                    bool part1 = s.Poll(1000, SelectMode.SelectRead);
                    bool part2 = (s.Available == 0);
                    if (part1 && part2)
                        return false;
                    else
                        return true;
                }
                else return false;
            }
            catch (SocketException se)
            {
                Console.WriteLine("socketconnectedfunc2" + se.ErrorCode);
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine("socketConnectedFunc " + e.Message);
                return false;
            }
        }


        public void ClipBoardProcessing()
        {
            Window.writeIpWindow(null, GetLocalIPAddress());
            while (accepting)
            {
                try
                {
                    tclient = myList.AcceptTcpClient();
                    tclient.GetStream().ReadTimeout = Timeout.Infinite;
                    tclient.Client.LingerState = new LingerOption(true, 0);
                    SetTcpKeepAlive(tclient.Client, 3000, 1);
                }
                catch (Exception e)
                {
                    Console.WriteLine("clipBoardProcfunc " + e.Message);
                    stop();
                    break;
                }
                Boolean logged = connectAndLogin();
                if (logged)
                {
                    IPEndPoint remoteIpEndPoint = tclient.Client.RemoteEndPoint as IPEndPoint;
                    IPEndPoint localIpEndPoint = tclient.Client.LocalEndPoint as IPEndPoint;
                    Thread t1 = new Thread(mcb.InitializeShare);
                    t1.Start();
                    Thread t2 = new Thread(mcb.AddConnection);
                    t2.Start((remoteIpEndPoint).Address.ToString());


                    if (remoteIpEndPoint != null && localIpEndPoint != null)
                    {
                        Window.writeIpWindow(remoteIpEndPoint.Address.ToString(), null);
                    }

                    Window.setPlayIcon();

                    while (true)
                    {
                        byte[] b = new byte[4];
                        try
                        {
                            if (tclient != null && SocketConnected(tclient.Client))
                            {
                                tclient.GetStream().Read(b, 0, 4);
                            }
                            else
                            {
                                Window.ConnectionClosed();
                                break;

                            }
                            switch (Convert.ToInt32(b[0]))
                            {

                                case 2:
                                    Console.WriteLine("send clipboard to client");
                                    byte[] clip = mcb.GetClipboardData();
                                    byte[] len = BitConverter.GetBytes(clip != null ? clip.Length : 0);
                                    tclient.GetStream().Write(len, 0, sizeof(int));
                                    if (clip != null)
                                    {
                                        tclient.GetStream().Write(clip, 0, clip.Length);
                                    }
                                    Window.setPlayIcon();
                                    break;

                                case 3:
                                    Console.WriteLine("get clipboard from client");

                                    byte[] recClipLen = new byte[sizeof(int)];
                                    tclient.GetStream().Read(recClipLen, 0, sizeof(int));
                                    int recLen = BitConverter.ToInt32(recClipLen, 0);
                                    if (recLen > 0)
                                    {
                                        int read = 0;
                                        byte[] recClip = new byte[recLen];
                                        while (read < recLen)
                                        {
                                            read += tclient.GetStream().Read(recClip, read, recLen - read);
                                        }

                                        using (var memStream = new MemoryStream())
                                        {
                                            var binForm = new BinaryFormatter();
                                            memStream.Write(recClip, 0, recClip.Length);
                                            memStream.Seek(0, SeekOrigin.Begin);
                                            var obj = binForm.Deserialize(memStream);
                                            Window.setRecIcon();
                                            Window.Dispatcher.Invoke(new Action(() =>
                                            {
                                                mcb.SetClipboard(
                                                    ((IPEndPoint)tclient.Client.RemoteEndPoint).Address
                                                        .ToString(), obj);

                                            }));
                                        }
                                    }


                                    break;


                            }
                        }
                        catch (SocketException se)
                        {


                            Window.ConnectionClosed();

                            Console.WriteLine("Error cristo " + se.ErrorCode);
                            break;
                        }
                        catch (IOException ie)
                        {

                            Window.ConnectionClosed();
                            Console.WriteLine("IOex " + ie.Message);
                            return;
                        }
                        catch (Exception e)
                        {

                            Window.ConnectionClosed();

                            Console.WriteLine("Error clipboardproc " + e.Message);
                            break;

                        }


                    }

                }
            }


        }
        public void InputProcessing()
        {

            IPEndPoint ip = new IPEndPoint(IPAddress.Any, 0);
            while (true)
            {

                byte[] data;

                MemoryStream messageStream = new MemoryStream();

                try
                {

                    data = uclient.Receive(ref ip);

                    messageStream.Write(data, sizeof(Int32), 28);

                    switch (Convert.ToInt32(data[0]))
                    {
                        case 0:
                            input.event_Switch_Mouse(messageStream.GetBuffer());
                            break;
                        case 1:
                            input.event_Switch_Keyboard(messageStream.GetBuffer());

                            break;

                    }


                }

                catch (Exception se)
                {

                    Window.ConnectionClosed();


                    Console.WriteLine("Error inputprocessing " + se.Message);
                    return;
                }


            }



        }


        private static void SetTcpKeepAlive(Socket socket, uint keepaliveTime, uint keepaliveInterval)
        {
            /* the native structure
            struct tcp_keepalive {
            ULONG onoff;
            ULONG keepalivetime;
            ULONG keepaliveinterval;
            };
            */

            // marshal the equivalent of the native structure into a byte array
            uint dummy = 0;
            byte[] inOptionValues = new byte[Marshal.SizeOf(dummy) * 3];
            BitConverter.GetBytes((uint)(keepaliveTime)).CopyTo(inOptionValues, 0);
            BitConverter.GetBytes((uint)keepaliveTime).CopyTo(inOptionValues, Marshal.SizeOf(dummy));
            BitConverter.GetBytes((uint)keepaliveInterval).CopyTo(inOptionValues, Marshal.SizeOf(dummy) * 2);

            // write SIO_VALS to Socket IOControl
            socket.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
        }

        public void stop()
        {
            try
            {
                accepting = false;
                Thread t = new Thread(mcb.DeleteShare);
                t.Start();

                Window.resetIpWindow(false);

                try { 
                if (tclient != null)
                    tclient.Client.Send(new byte[1]);
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                if (myList != null)
                    myList.Stop();

                if (tclient != null)
                {
                    tclient.Client.Shutdown(SocketShutdown.Receive);
                    tclient.Client.Close();
                    tclient.Close();

                }

                if (uclient != null)
                    uclient.Close();


                uclient = null;
                myList = null;
                tclient = null;
            }

            catch (Exception e)
            {
                myList = null;
                tclient = null;
                uclient = null;

                Console.WriteLine("stopfunc " + e.Message);
            }

        }


    }


}

