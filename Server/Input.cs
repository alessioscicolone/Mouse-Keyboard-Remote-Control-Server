using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Server
{
    class Input
    {

        private const UInt32 MOUSEEVENTF_ABSOLUTE = 0x8000;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const UInt32 MOUSEEVENTF_MOVE = 0x0001;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_XDOWN = 0x0080;
        private const uint MOUSEEVENTF_XUP = 0x0100;
        private const uint MOUSEEVENTF_WHEEL = 0x0800;
        private const uint MOUSEEVENTF_HWHEEL = 0x01000;

        private const uint WM_LBUTTONDBLCLK = 0x0203;
        private const uint WM_LBUTTONDOWN = 0x0201;
        private const uint WM_LBUTTONUP = 0x0202;
        private const uint WM_MBUTTONDBLCLK = 0x0209;
        private const uint WM_MBUTTONDOWN = 0x0207;
        private const uint WM_MBUTTONUP = 0x0208;
        private const uint WM_MOUSEMOVE = 0x0200;
        private const uint WM_MOUSEWHEEL = 0x020A;
        private const uint WM_RBUTTONDOWN = 0x0204;
        private const uint WM_RBUTTONDBLCLK = 0x0206;
        private const uint WM_RBUTTONUP = 0x0205;
        private const uint WM_XBUTTONDBLCLK = 0x020D;
        private const uint WM_XBUTTONDOWN = 0x020B;
        private const uint WM_XBUTTONUP = 0x020C;
        private const uint WM_MOUSEHWHEEL = 0x020E;

        private const int WM_KEYDOWN = 0x100;
        private const int WM_KEYUP = 0x101;
        private const int WM_SYSKEYUP = 0x0105;
        private const int WM_SYSKEYDOWN = 0x0104;


        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        static extern uint keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);


        //Declare the wrapper managed POINT class.
        [StructLayout(LayoutKind.Sequential)]
        public class POINT
        {
            public uint x;
            public uint y;
        }

        //Declare the wrapper managed MouseHookStruct class.
        [StructLayout(LayoutKind.Sequential)]
        public class MouseHookStruct
        {
            public POINT pt;
            public int mouseData;
            public int hwnd;
            public int wHitTestCode;
            public int dwExtraInfo;
        }

        struct MouseStruct
        {
            public MouseHookStruct mhs;
            public int me;
        }

        struct KeyboardStruct
        {
            public IntPtr wparam;
            public KBDLLHOOKSTRUCT kb;
        }

        private struct KBDLLHOOKSTRUCT
        {
            internal int vkCode;
            internal int scanCode;
            internal int flags;
            internal int time;
            internal int dwExtraInfo;
        }

        MouseStruct mfromBytes(byte[] arr)
        {
            MouseStruct mystruct = new MouseStruct();

            int size = Marshal.SizeOf(mystruct);
            IntPtr ptr = Marshal.AllocHGlobal(size);

            Marshal.Copy(arr, 0, ptr, size);

            mystruct = (MouseStruct)Marshal.PtrToStructure(ptr, mystruct.GetType());
            Marshal.FreeHGlobal(ptr);
            return mystruct;
        }

        KeyboardStruct kfromBytes(byte[] arr)
        {

            KeyboardStruct mystruct = new KeyboardStruct();
            int size = Marshal.SizeOf(mystruct);
            IntPtr ptr = Marshal.AllocHGlobal(size);

            Marshal.Copy(arr, 0, ptr, size);

            mystruct = (KeyboardStruct)Marshal.PtrToStructure(ptr, mystruct.GetType());
            Marshal.FreeHGlobal(ptr);
            return mystruct;
        }

        public void event_Switch_Mouse(byte[] b1)//uint caseSwitch, POINT p, int rotation)
        {
            MouseStruct mystruct = mfromBytes(b1);
            POINT p = normalizatonPoint(mystruct.mhs.pt);
            int rotation = mystruct.mhs.mouseData;
            switch ((uint)mystruct.me)
            {
                case WM_LBUTTONDBLCLK:
                    Console.WriteLine("Case 1");
                    sendMouseDoubleClick(p);
                    break;
                case WM_LBUTTONDOWN:
                    Console.WriteLine("Case 2");
                    sendMouseDown(p);
                    break;
                case WM_LBUTTONUP:
                    Console.WriteLine("Case3");
                    sendMouseUp(p);
                    break;
                case WM_MBUTTONDBLCLK:
                    Console.WriteLine("Case 4");
                    sendMouseMDoubleClick(p);
                    break;
                case WM_MBUTTONDOWN:
                    Console.WriteLine("Case 5");
                    sendMouseMDown(p);
                    break;
                case WM_MBUTTONUP:
                    Console.WriteLine("Case 6");
                    sendMouseMUp(p);
                    break;
                case WM_MOUSEMOVE:
                    Console.WriteLine("Case 7");
                    moveMouse(p);
                    break;
                case WM_MOUSEWHEEL:
                    Console.WriteLine("Case 8 wheel");
                    mouveWHEELMouse(p, (uint)rotation);
                    break;
                case WM_RBUTTONDOWN:
                    Console.WriteLine("Case 9");
                    sendMouseRightDown(p);
                    break;
                case WM_RBUTTONDBLCLK:
                    Console.WriteLine("Case 10");
                    sendMouseRightDoubleClick(p);
                    break;
                case WM_RBUTTONUP:
                    Console.WriteLine("Case 11");
                    sendMouseRightUp(p);
                    break;
                case WM_XBUTTONDBLCLK:
                    Console.WriteLine("Case 12");
                    sendMouseXDoubleClick(p);
                    break;
                case WM_XBUTTONDOWN:
                    Console.WriteLine("Case 13");
                    sendMouseXDown(p);
                    break;
                case WM_XBUTTONUP:
                    Console.WriteLine("Case 14");
                    sendMouseXUp(p);
                    break;
                case WM_MOUSEHWHEEL:
                    Console.WriteLine("Case 14 wheel");
                    mouveHWHEELMouse(p, (uint)rotation);
                    break;
                default:
                    Console.WriteLine("Default case mouse" + (uint)mystruct.me);
                    break;
            }
        }


        //*************************KEYBOARD***************************
        public static void KeyDown(int key)
        {
            keybd_event((byte)key, 0x45, 0, 0);
        }

        public static void KeyUp(int key)
        {
            keybd_event((byte)key, 0x45, 0x0002, 0);
        }

        //***************************MOUSE*****************************

        void moveMouse(POINT p)
        {
            mouse_event(MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE, p.x, p.y, 0, UIntPtr.Zero);
        }

        void mouveWHEELMouse(POINT p, uint rotation)
        {
            mouse_event(MOUSEEVENTF_WHEEL | MOUSEEVENTF_ABSOLUTE, p.x, p.y, rotation, UIntPtr.Zero);
        }

        void mouveHWHEELMouse(POINT p, uint rotation)
        {
            mouse_event(MOUSEEVENTF_HWHEEL | MOUSEEVENTF_ABSOLUTE, p.x, p.y, rotation, UIntPtr.Zero);
        }

        void sendMouseDoubleClick(POINT p)
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP | MOUSEEVENTF_ABSOLUTE, p.x, p.y, 0, UIntPtr.Zero);

            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP | MOUSEEVENTF_ABSOLUTE, p.x, p.y, 0, UIntPtr.Zero);
        }

        void sendMouseRightDoubleClick(POINT p)
        {
            mouse_event(MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_RIGHTUP | MOUSEEVENTF_ABSOLUTE, p.x, p.y, 0, UIntPtr.Zero);

            mouse_event(MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_RIGHTUP | MOUSEEVENTF_ABSOLUTE, p.x, p.y, 0, UIntPtr.Zero);
        }

        void sendMouseMDoubleClick(POINT p)
        {
            mouse_event(MOUSEEVENTF_MIDDLEDOWN | MOUSEEVENTF_MIDDLEUP | MOUSEEVENTF_ABSOLUTE, p.x, p.y, 0, UIntPtr.Zero);

            mouse_event(MOUSEEVENTF_MIDDLEDOWN | MOUSEEVENTF_MIDDLEUP | MOUSEEVENTF_ABSOLUTE, p.x, p.y, 0, UIntPtr.Zero);
        }

        void sendMouseXDoubleClick(POINT p)
        {
            mouse_event(MOUSEEVENTF_XDOWN | MOUSEEVENTF_XUP | MOUSEEVENTF_ABSOLUTE, p.x, p.y, 0, UIntPtr.Zero);

            mouse_event(MOUSEEVENTF_XDOWN | MOUSEEVENTF_XUP | MOUSEEVENTF_ABSOLUTE, p.x, p.y, 0, UIntPtr.Zero);
        }

        void sendMouseDown(POINT p)
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_ABSOLUTE, p.x, p.y, 0, UIntPtr.Zero);
        }

        void sendMouseUp(POINT p)
        {
            mouse_event(MOUSEEVENTF_LEFTUP | MOUSEEVENTF_ABSOLUTE, p.x, p.y, 0, UIntPtr.Zero);
        }

        void sendMouseRightDown(POINT p)
        {
            mouse_event(MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_ABSOLUTE, p.x, p.y, 0, UIntPtr.Zero);
        }

        void sendMouseRightUp(POINT p)
        {
            mouse_event(MOUSEEVENTF_RIGHTUP | MOUSEEVENTF_ABSOLUTE, p.x, p.y, 0, UIntPtr.Zero);
        }

        void sendMouseXDown(POINT p)
        {
            mouse_event(MOUSEEVENTF_XDOWN | MOUSEEVENTF_ABSOLUTE, p.x, p.y, 0, UIntPtr.Zero);
        }

        void sendMouseXUp(POINT p)
        {
            mouse_event(MOUSEEVENTF_XUP | MOUSEEVENTF_ABSOLUTE, p.x, p.y, 0, UIntPtr.Zero);
        }

        void sendMouseMDown(POINT p)
        {
            mouse_event(MOUSEEVENTF_MIDDLEDOWN | MOUSEEVENTF_ABSOLUTE, p.x, p.y, 0, UIntPtr.Zero);
        }

        void sendMouseMUp(POINT p)
        {
            mouse_event(MOUSEEVENTF_MIDDLEUP | MOUSEEVENTF_ABSOLUTE, p.x, p.y, 0, UIntPtr.Zero);
        }


        //**************************Normalization***************************
        POINT normalizatonPoint(POINT p)
        {
            Double w = System.Windows.SystemParameters.PrimaryScreenWidth;
            Double h = System.Windows.SystemParameters.PrimaryScreenHeight;

            p.x = (uint)((p.x / w) * 65535);
            p.y = (uint)((p.y / h) * 65535);



            return p;
        }

        public void event_Switch_Keyboard(byte[] b1)//uint caseSwitch, int key)
        {
            KeyboardStruct mystruct = kfromBytes(b1);
            int key = mystruct.kb.vkCode;

            switch ((uint)mystruct.wparam)
            {
                case WM_KEYUP:
                    Console.WriteLine("up" + key);
                    KeyUp(key);
                    break;
                case WM_KEYDOWN:
                    Console.WriteLine("down" + key);
                    KeyDown(key);
                    break;
                case WM_SYSKEYDOWN:
                    Console.WriteLine("sysdown" + key);
                    KeyDown(key);
                    break;
                case WM_SYSKEYUP:
                    Console.WriteLine("sysup" + key);
                    KeyUp(key);
                    break;
                default:
                    Console.WriteLine("Default case keyboard" + key);
                    Console.WriteLine("Default case keyboard" + (uint)mystruct.wparam);
                    break;
            }
        }

    }
}
