using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Listik
{
    internal class KeyHack : IDisposable
    {
        private static MainWindow _staticMainWindow;
        public KeyHack(MainWindow mainWindow)
        {
            _staticMainWindow = mainWindow;
            _hookID = SetHook(_proc);
        }
        public void Dispose()
        {
           
            GC.SuppressFinalize(this);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
       LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;
        static Form _form;

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);


        private static KeyHack _instance; // Для доступа из статического метода
        public event Action<string> KeyPressed;
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
          {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                KeysConverter converter = new KeysConverter();
                string keys = converter.ConvertToString((Keys)vkCode);

                //if (keys == _form._appSettings.Key_FullTundra)
                //    EnableFullTundra();
                //if (keys == _form._appSettings.Key_TreeTundra)
                //    EnableTreeTundra();
                //if (keys == _form._appSettings.Key_KustTundra)
                //    EnableKustTundra();
                _staticMainWindow?.IsPressHotkey(keys);
           
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        public void UnHookKeys()
        {
            UnhookWindowsHookEx(_hookID);
        }
    }
}
