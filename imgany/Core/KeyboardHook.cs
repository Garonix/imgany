
using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Text;

namespace imgany.Core
{
    public class KeyboardHook : IDisposable
    {
        private NativeMethods.LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;

        public event EventHandler PasteDetected;

        public KeyboardHook()
        {
            _proc = HookCallback;
        }

        public void Start()
        {
            if (_hookID == IntPtr.Zero)
            {
                using (Process curProcess = Process.GetCurrentProcess())
                using (ProcessModule curModule = curProcess.MainModule)
                {
                    _hookID = NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, _proc,
                        NativeMethods.GetModuleHandle(curModule.ModuleName), 0);
                }
            }
        }

        public void Stop()
        {
            if (_hookID != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)NativeMethods.WM_KEYDOWN || wParam == (IntPtr)NativeMethods.WM_SYSKEYDOWN))
            {
                int vkCode = Marshal.ReadInt32(lParam);

                if (vkCode == NativeMethods.VK_V)
                {
                    // Check logic: Ctrl pressed?
                    bool ctrlDown = (NativeMethods.GetKeyState(NativeMethods.VK_CONTROL) & 0x8000) != 0;
                    
                    if (ctrlDown)
                    {
                        // Check logic: Active Window is Explorer?
                        if (IsExplorerActive())
                        {
                            // Fire event - if handled, swallow key
                            // We need a way to return 1 if we handled it.
                            // Simplified for now: assume we might handle it. 
                            // In real arch, event args should allow cancellation.
                            
                            // Let's create a custom arg if needed, but for now just fire.
                            // NOTE: If we want to blocking-ly swallow, we need to do it here.
                            
                            var args = new PasteHookEventArgs();
                            PasteDetected?.Invoke(this, args);
                            
                            if (args.Handled)
                            {
                                return (IntPtr)1; // Swallow
                            }
                        }
                    }
                }
            }
            return NativeMethods.CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private bool IsExplorerActive()
        {
            IntPtr checkHwnd = NativeMethods.GetForegroundWindow();
            if (checkHwnd == IntPtr.Zero) return false;

            StringBuilder sb = new StringBuilder(256);
            NativeMethods.GetClassName(checkHwnd, sb, sb.Capacity);
            string className = sb.ToString();

            // CabinetWClass = Explorer Main Window
            // ExploreWClass = Legacy Explorer?
            // WorkerW = Desktop
            // Progman = Desktop
            return className == "CabinetWClass" || className == "ExploreWClass" || className == "Progman" || className == "WorkerW";
        }

        public void Dispose()
        {
            Stop();
        }
    }

    public class PasteHookEventArgs : EventArgs
    {
        public bool Handled { get; set; } = false;
    }
}
