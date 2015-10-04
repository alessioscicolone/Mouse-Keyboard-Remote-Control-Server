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
        private Socket s;
        private IPEndPoint localpt;
        private MyClipBoard mcb;
        public MainWindow Window { get; set; }

        private byte[] publicKey;
        private ECDiffieHellmanCng exch;
        private bool logged = false;
        private string username;
        private string password;
        private Boolean accepting = true;


        public MyServer(int port, string username, string password)
        {
            try
            {
                exch = new ECDiffieHellmanCng(256);
                exch.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
                exch.HashAlgorithm = CngAlgorithm.Sha256;
                publicKey = exch.PublicKey.ToByteArray();
                this.username = username;
                this.password = password;
                input = new Input();
                mcb = new MyClipBoard();
                localpt = new IPEndPoint(IPAddress.Any, port);
                /* Initializes the Listener */
                myList = new TcpListener(localpt);
                /* Start Listening at the specified port */
                myList.Start();
                Console.WriteLine("The server is running at port 8001...");
                Console.WriteLine("The local End point is :" + myList.LocalEndpoint);
                Console.WriteLine("Waiting for a connection.....");

                Thread t = new Thread(InputProcessing);
                t.SetApartmentState(ApartmentState.STA);
                t.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);             
            }


        }

        public void startProcessing()
        {
            try
            {
                s = myList.AcceptSocket();
                Console.WriteLine("Connection accepted from " + s.RemoteEndPoint);
                s.ReceiveTimeout = Timeout.Infinite;
                s.LingerState = new LingerOption(true, 0);

                Boolean logged = connectAndLogin();
                if (logged)
                {
                    Thread t1 = new Thread(mcb.InitializeShare);
                    t1.Start();
                    Thread t2 = new Thread(mcb.AddConnection);
                    t2.Start((s.RemoteEndPoint as IPEndPoint).Address.ToString());
                    Thread t = new Thread(InputProcessing);
                    t.SetApartmentState(ApartmentState.STA);
                    t.Start();
                }
                else
                {
                    Console.WriteLine("errore autenticazione");
                    s.Close();
                    myList.Stop();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("errore accept");
                s.Close();
                myList.Stop();

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

                int k = s.Receive(clientPublicKey, 72, SocketFlags.None);
                byte[] derivedKey =
                exch.DeriveKeyMaterial(CngKey.Import(clientPublicKey, CngKeyBlobFormat.EccPublicBlob));
                s.Send(publicKey, publicKey.Length, SocketFlags.None);
                StreamReader streamReader = new StreamReader(new NetworkStream(s));

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
                s.Send(auth, sizeof(bool), SocketFlags.None);
                return logged;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }

        }

        private bool SocketConnected(Socket s)
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
        public void InputProcessing()
        {
            while (accepting)
            {
                try { 
                s = myList.AcceptSocket();
                
                    Console.WriteLine("Connection accepted from " + s.RemoteEndPoint);
                }
                catch(Exception e)
                {
                    stop();
                    break;
                }
                Boolean logged = connectAndLogin();
                if (logged)
                {
                    IPEndPoint remoteIpEndPoint = s.RemoteEndPoint as IPEndPoint;
                    IPEndPoint localIpEndPoint = s.LocalEndPoint as IPEndPoint;
                    Thread t1 = new Thread(mcb.InitializeShare);
                    t1.Start();
                    Thread t2 = new Thread(mcb.AddConnection);
                    t2.Start((remoteIpEndPoint).Address.ToString());

                    if (remoteIpEndPoint != null && localIpEndPoint != null )
                    {
                        Window.Dispatcher.Invoke(new Action(() =>
                        {
                            Window.writeIpWindow(remoteIpEndPoint.Address.ToString(), localIpEndPoint.Address.ToString());
                        }));
                    }
                    while (true)
                    {
                        byte[] b = new byte[4];
                        byte[] b1 = new byte[28];
                        byte[] b2 = new byte[24];
                        try
                        {
                            if (SocketConnected(s))
                            {
                                int key = s.Receive(b, 4, SocketFlags.None);
                            }
                            else
                            {
                                Window.Dispatcher.Invoke(new Action(() =>
                                {
                                    Window.ConnectionClosed();
                                }));
                                break;

                            }

                            switch (Convert.ToInt32(b[0]))
                            {
                                case 0:
                                    s.Receive(b1, 28, SocketFlags.None);
                                    input.event_Switch_Mouse(b1);
                                    break;
                                case 1:
                                    s.Receive(b2, 24, SocketFlags.None);
                                    input.event_Switch_Keyboard(b2);
                                    break;
                                case 2:
                                    Console.WriteLine("send clipboard to client");
                                    byte[] clip = mcb.GetClipboardData();
                                    byte[] len = BitConverter.GetBytes(clip != null ? clip.Length : 0);
                                    s.Send(len, sizeof(int), SocketFlags.None);
                                    if (clip != null)
                                    {
                                        s.Send(clip, clip.Length, SocketFlags.None);
                                    }
                                    Window.Dispatcher.Invoke(new Action(() =>
                                      {
                                          Window.setPauseIcon();
                                      }));
                                    break;

                                case 3:
                                    Console.WriteLine("get clipboard from client");

                                    byte[] recClipLen = new byte[sizeof(int)];
                                    s.Receive(recClipLen, sizeof(int), SocketFlags.None);
                                    int recLen = BitConverter.ToInt32(recClipLen, 0);
                                    if (recLen > 0)
                                    {
                                        int read = 0;
                                        byte[] recClip = new byte[recLen];
                                        while (read < recLen)
                                        {
                                            read += s.Receive(recClip, read, recLen - read, SocketFlags.None);
                                        }

                                        using (var memStream = new MemoryStream())
                                        {
                                            var binForm = new BinaryFormatter();
                                            memStream.Write(recClip, 0, recClip.Length);
                                            memStream.Seek(0, SeekOrigin.Begin);
                                            var obj = binForm.Deserialize(memStream);

                                            Window.Dispatcher.Invoke(new Action(() =>
                                            {
                                                mcb.SetClipboard(
                                                    ((IPEndPoint)s.RemoteEndPoint).Address
                                                        .ToString(), obj);
                                                Window.setPlayIcon();
                                            }));
                                        }
                                    }


                                    break;

                            }


                        }

                        catch (Exception se)
                        {
                            if (!SocketConnected(s))
                            {
                                Window.Dispatcher.Invoke(new Action(() =>
                                {
                                    Window.ConnectionClosed();
                                }));
                                
                            }
                           
                            Console.WriteLine("Error " + se.Message);
                            break;
                        }


                    }
                }
            }
        }




        public void stop()
        {
            try
            {
                accepting = false;
                Thread t = new Thread(mcb.DeleteShare);
                t.Start();
                if(s != null)
                s.Close();
                myList.Stop();
                myList = null;
                s = null;
            }

            catch (Exception e)
            {
                myList = null;
                s = null;
            }

        }


    }


}

