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

            // 2. Iterate Windows to find the one matching our handle
            try
            {
                var windows = shell.Windows();
                foreach (dynamic window in windows)
                {
                    if (window.HWND == (long)handle) // HWND might be int or long depending on bitness
                    {
                        // Found it. Get the path.
                        // window.Document is IShellFolderViewDual
                        // window.Document.Folder is Folder
                        // window.Document.Folder.Self is FolderItem
                        // window.Document.Folder.Self.Path is string
                        
                        var doc = window.Document;
                        if (doc == null) continue;

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
            catch (Exception ex)
            {
                // COM errors happen. Ignore.
                Debug.WriteLine($"Explorer Interop Error: {ex.Message}");
            }

            return null; // Not found or not an explorer window
        }
    }
}
