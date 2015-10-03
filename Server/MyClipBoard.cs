using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Server
{
    class MyClipBoard
    {

        [DllImport("Netapi32.dll")]
        private static extern uint NetShareAdd(
            [MarshalAs(UnmanagedType.LPWStr)] string strServer,
            Int32 dwLevel,
            ref SHARE_INFO_502 buf,
            out uint parm_err
        );

        [DllImport("netapi32.dll")]
        static extern uint NetShareDel(
                    [MarshalAs(UnmanagedType.LPWStr)] string strServer,
                    [MarshalAs(UnmanagedType.LPWStr)] string strNetName,
                    Int32 reserved //must be 0
                    );

        [DllImport("netapi32.dll", SetLastError = true)]
        static extern uint NetShareCheck(
            [MarshalAs(UnmanagedType.LPWStr)] string servername,
            [MarshalAs(UnmanagedType.LPWStr)] string device,
            out SHARE_TYPE type
            );

        private enum NetError : uint
        {
            NERR_Success = 0,
            NERR_BASE = 2100,
            NERR_UnknownDevDir = (NERR_BASE + 16),
            NERR_DuplicateShare = (NERR_BASE + 18),
            NERR_BufTooSmall = (NERR_BASE + 23),
            NERR_DeviceNotShared = 0x00000907
        }

        [Flags]
        private enum SHARE_TYPE : uint
        {
            STYPE_DISKTREE = 0,
            STYPE_PRINTQ = 1,
            STYPE_DEVICE = 2,
            STYPE_IPC = 3,
            STYPE_TEMPORARY = 0x40000000,
            STYPE_SPECIAL = 0x80000000,
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SHARE_INFO_502
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string shi502_netname;
            public SHARE_TYPE shi502_type;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string shi502_remark;
            public Int32 shi502_permissions;
            public Int32 shi502_max_uses;
            public Int32 shi502_current_uses;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string shi502_path;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string shi502_passwd;
            public Int32 shi502_reserved;
            public IntPtr shi502_security_descriptor;
        }

        [DllImport("mpr.dll")]
        private static extern int WNetAddConnection2(NetResource netResource,
            string password, string username, int flags);

        [DllImport("mpr.dll")]
        private static extern int WNetCancelConnection2(string name, int flags,
            bool force);

        [StructLayout(LayoutKind.Sequential)]
        public class NetResource
        {
            public ResourceScope Scope;
            public ResourceType ResourceType;
            public ResourceDisplaytype DisplayType;
            public int Usage;
            public string LocalName;
            public string RemoteName;
            public string Comment;
            public string Provider;
        }

        public enum ResourceScope : int
        {
            Connected = 1,
            GlobalNetwork,
            Remembered,
            Recent,
            Context
        };

        public enum ResourceType : int
        {
            Any = 0,
            Disk = 1,
            Print = 2,
            Reserved = 8,
        }

        public enum ResourceDisplaytype : int
        {
            Generic = 0x0,
            Domain = 0x01,
            Server = 0x02,
            Share = 0x03,
            File = 0x04,
            Group = 0x05,
            Network = 0x06,
            Root = 0x07,
            Shareadmin = 0x08,
            Directory = 0x09,
            Tree = 0x0a,
            Ndscontainer = 0x0b
        }


        public void InitializeShare()
        {
            SHARE_TYPE type;
            DriveInfo[] allDrives = DriveInfo.GetDrives();
            foreach (DriveInfo d in allDrives)
            {

                string shareName = d.Name.Replace(":\\", "");
                string shareDesc = "";
                string path = d.Name;

                SHARE_INFO_502 info = new SHARE_INFO_502();
                info.shi502_netname = shareName;
                info.shi502_type = SHARE_TYPE.STYPE_DISKTREE | SHARE_TYPE.STYPE_TEMPORARY;
                info.shi502_remark = shareDesc;
                info.shi502_permissions = 0;
                info.shi502_max_uses = -1;
                info.shi502_current_uses = 0;
                info.shi502_path = path;
                info.shi502_passwd = null;
                info.shi502_reserved = 0;
                info.shi502_security_descriptor = IntPtr.Zero;

                uint error = 0;
                uint result;
                if ((result = NetShareAdd(null, 502, ref info, out error)) != 0)
                {
                    Console.WriteLine("result = " + result + " error = " + error);
                }

            }
        }


        public void DeleteShare()
        {
            SHARE_TYPE type;
            DriveInfo[] allDrives = DriveInfo.GetDrives();
            foreach (DriveInfo d in allDrives)
            {

                string shareName = d.Name.Replace(":\\", "");
                string shareDesc = "";
                string path = d.Name;
                uint result;
                if ((result = NetShareDel(null, shareName, 0)) != 0)
                {
                    Console.WriteLine("delete result: " + result);
                }
            }
        }


        public void AddConnection(object ip)
        {
            var netResource = new NetResource()
            {
                Scope = ResourceScope.GlobalNetwork,
                ResourceType = ResourceType.Disk,
                DisplayType = ResourceDisplaytype.Share,
                RemoteName = "\\\\" + (string)ip + "\\C"
            };

            var result = WNetAddConnection2(
            netResource,
            null,
            null,
            0x00000004 | 0x00000008 | 0x1000);

            if (result != 0)
            {
                Console.WriteLine("Result not zero: " + result);
            }
        }

        public void SetClipboard(string Ip, Object obj)
        {
            try
            {
                ArrayList data = (ArrayList)obj;
                if (data != null)
                {
                    DataObject dataObj = new DataObject();
                    Console.WriteLine("Count: " + data.Count);
                    for (int i = 0; i < data.Count; i++)
                    {
                        string format = (string)data[i++];
                        Console.WriteLine(format);
                        dataObj.SetData(format, data[i]);
                    }
                    if (dataObj.ContainsFileDropList())
                    {
                        StringCollection files = dataObj.GetFileDropList();
                        dataObj = new DataObject();
                        StringCollection adjusted = new StringCollection();
                        foreach (string f in files)
                        {
                            if (!f.StartsWith("\\"))
                            {
                                string toadd = "\\\\" + Ip + "\\" + f.Replace(":", "");
                                Console.WriteLine(toadd);
                                adjusted.Add(toadd);
                            }
                            else
                            {
                                adjusted.Add(f);
                            }
                        }
                        dataObj.SetFileDropList(adjusted);
                    }
                    Clipboard.SetDataObject(dataObj);


                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }


        public byte[] GetClipboardData()
        {
            try
            {
                IDataObject data = Clipboard.GetDataObject();
                ArrayList dataObjects = new ArrayList();
                if (data != null)
                {

                    string[] formats = data.GetFormats();
                    BinaryFormatter bf = new BinaryFormatter();
                    for (int i = 0; i < formats.Length; i++)
                    {
                        object clipboardItem;
                        try
                        {
                            clipboardItem = data.GetData(formats[i]);
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                        if (clipboardItem != null && clipboardItem.GetType().IsSerializable)
                        {
                            Console.WriteLine("sending {0}", formats[i]);
                            dataObjects.Add(formats[i]);
                            dataObjects.Add(clipboardItem);
                        }
                        else
                            Console.WriteLine("ignoring {0}", formats[i]);
                    }
                    using (var ms = new MemoryStream())
                    {
                        bf.Serialize(ms, dataObjects);
                        return ms.ToArray();
                    }
                }
                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }


    }
}
