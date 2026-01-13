using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text;

namespace imgany.Core
{
    public static class ExplorerInterop
    {
        public static string GetActiveExplorerPath()
        {
            IntPtr handle = NativeMethods.GetForegroundWindow();
            if (handle == IntPtr.Zero) return null;

            // 1. Get Shell Object
            Type shellType = Type.GetTypeFromProgID("Shell.Application");
            if (shellType == null) return null;

            dynamic shell = Activator.CreateInstance(shellType);
            if (shell == null) return null;

            // 2. Identify the focused window within the foreground thread
            uint processId;
            uint threadId = NativeMethods.GetWindowThreadProcessId(handle, out processId);

            NativeMethods.GUITHREADINFO guiInfo = new NativeMethods.GUITHREADINFO();
            guiInfo.cbSize = Marshal.SizeOf(guiInfo);

            IntPtr hwndFocus = IntPtr.Zero;
            if (NativeMethods.GetGUIThreadInfo(threadId, ref guiInfo))
            {
                hwndFocus = guiInfo.hwndFocus;
            }

            // 3. Iterate Windows to find the one matching our handle
            try
            {
                var windows = shell.Windows();
                foreach (dynamic window in windows)
                {
                    if (window.HWND == (long)handle) // HWND might be int or long depending on bitness
                    {
                        // We found an explorer window with the matching top-level HWND.
                        // Now we need to check if this specific "window" (tab) is the one that has focus.
                        
                        var doc = window.Document;
                        if (doc == null) continue;

                        // Check if this window's view considers itself focused or contains the focus
                        if (IsWindowActiveTab(window, hwndFocus))
                        {
                            var folder = doc.Folder;
                            if (folder == null) continue;

                            var self = folder.Self;
                            if (self == null) continue;

                            string path = self.Path;

                            // Validate it's a file system path
                            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                            {
                                return path;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // COM errors happen. Ignore.
                Debug.WriteLine($"Explorer Interop Error: {ex.Message}");
            }

            return null; // Not found or not an explorer window
        }

        private static bool IsWindowActiveTab(dynamic window, IntPtr hwndFocus)
        {
            if (hwndFocus == IntPtr.Zero)
            {
                // Fallback: if we couldn't get focus info, assume the first one we find is "it"
                // This mimics old behavior but it's better than crashing.
                // Or maybe we should return true?
                // Let's return true to be safe for non-tabbed explorers (Win10) or weird edge cases.
                return true;
            }

            try
            {
                // Query IServiceProvider -> IShellBrowser -> IShellView -> GetWindow
                // We need to do some customized COM casting here.
                
                // 'window' is IWebBrowser2.
                // We need to cast it to IServiceProvider.
                // Since 'window' is dynamic (RCW), we can cast it directly?
                // No, we need to declare the interface to cast to it cleanly.

                IServiceProvider sp = window as IServiceProvider;
                if (sp != null)
                {
                    Guid guidIShellBrowser = typeof(IShellBrowser).GUID;
                    Guid guidIShellBrowserInterface = typeof(IShellBrowser).GUID;
                    object sbObj;
                    sp.QueryService(ref guidIShellBrowser, ref guidIShellBrowserInterface, out sbObj);

                    IShellBrowser shellBrowser = sbObj as IShellBrowser;
                    if (shellBrowser != null)
                    {
                        IntPtr hwndView;
                        shellBrowser.GetWindow(out hwndView);

                        if (hwndView == hwndFocus || NativeMethods.IsChild(hwndView, hwndFocus))
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"IsWindowActiveTab checking failed: {ex.Message}");
            }

            return false;
        }

        // COM Interfaces definitions

        [ComImport]
        [Guid("6d5140c1-7436-11ce-8034-00aa006009fa")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IServiceProvider
        {
            void QueryService(ref Guid guidService, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppvObject);
        }

        [ComImport]
        [Guid("000214E2-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IShellBrowser
        {
            void GetWindow(out IntPtr phwnd);
            // We only need GetWindow, but strictly speaking VTable order matters.
            // IShellBrowser inherits from IOleWindow.
            // IOleWindow has: GetWindow, ContextSensitiveHelp.
            // So GetWindow is the first method. Correct.
            
            // ... other methods omitted ...
        }
    }
}
