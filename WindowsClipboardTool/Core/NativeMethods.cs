using System;
using System.Runtime.InteropServices;
using System.Text;

namespace WindowsClipboardTool.Core
{
    public static class NativeMethods
    {
        // Hook
        public const int WH_KEYBOARD_LL = 13;
        public const int WM_KEYDOWN = 0x0100;
        public const int WM_SYSKEYDOWN = 0x0104;

        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        // Window Info
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        // Keys
        [DllImport("user32.dll")]
        public static extern short GetKeyState(int nVirtKey);

        public const int VK_CONTROL = 0x11;
        public const int VK_V = 0x56;

        // Clipboard
        [DllImport("user32.dll")]
        public static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        public const int WM_CLIPBOARDUPDATE = 0x031D;
    }
}
