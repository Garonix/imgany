using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Security.Principal;
using System.Windows.Forms;

namespace WindowsClipboardTool.Core
{
    public static class TaskSchedulerManager
    {
        private const string TaskName = "WindowsClipboardTool_AutoStart";

        public static bool IsTaskRegistered()
        {
            try
            {
                // Check if task exists using schtasks /Query
                var psi = new ProcessStartInfo
                {
                    FileName = "schtasks",
                    Arguments = $"/Query /TN \"{TaskName}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                
                using (var p = Process.Start(psi))
                {
                    p.WaitForExit();
                    return p.ExitCode == 0;
                }
            }
            catch { return false; }
        }

        public static void RegisterTask()
        {
            string exePath = Application.ExecutablePath;
            // ONLOGON trigger. 
            // NOTE: Requires RunAs Administrator permissions usually to create tasks for all users, 
            // but for current user "/Create" usually works if we don't use /RU SYSTEM.
            // Using /RL HIGHEST to ensure it runs with privileges if possible, or standard.
            // Argument: /SC ONLOGON /TR ... 
            
            var psi = new ProcessStartInfo
            {
                FileName = "schtasks",
                Arguments = $"/Create /SC ONLOGON /TN \"{TaskName}\" /TR \"\\\"{exePath}\\\"\" /RL HIGHEST /F",
                CreateNoWindow = true,
                UseShellExecute = true, // To trigger UAC if needed? No, silent best.
                Verb = "runas" // Try to force Admin if needed
            };

            // NOTE: Running schtasks often triggers a CMD window flash if not careful.
            // We use ShellExecute=true + runas to ask for permission if needed, 
            // OR use ShellExecute=false + CreateNoWindow=true for silent operation (might fail if no admin).
            
            // Let's try silent first.
            psi.UseShellExecute = false; 
            psi.Verb = "";
            
            using (var p = Process.Start(psi))
            {
                p.WaitForExit();
            }
        }

        public static void UnregisterTask()
        {
            var psi = new ProcessStartInfo
            {
                FileName = "schtasks",
                Arguments = $"/Delete /TN \"{TaskName}\" /F",
                CreateNoWindow = true,
                UseShellExecute = false
            };
            
            using (var p = Process.Start(psi))
            {
                p.WaitForExit();
            }
        }
    }

    public static class IconHelper
    {
        // Generates status icons programmatically
        public static Icon CreateStatusIcon(Color color, string symbol)
        {
            // Size 16x16 or 32x32. Tray usually 16.
            // Let's make 32 for HighDPI.
            int size = 32;
            using (Bitmap bmp = new Bitmap(size, size))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // Draw background circle or rounded square
                using (Brush b = new SolidBrush(color))
                {
                    // Rounded rect
                    // g.FillEllipse(b, 1, 1, size-2, size-2);
                   
                   // Draw rounded square for modern look
                   int radius = 8;
                   Rectangle rect = new Rectangle(1, 1, size-2, size-2);
                   // Simple approximation of rounded rect or just circle. Circle looks better in tray.
                   // User asked for "Stylish blue clipboard". Tray icons are small.
                   // Circle is safer for consistency.
                   g.FillEllipse(b, rect);
                }

                // Draw Symbol
                // Checkmark or X or Dot
                using (Pen p = new Pen(Color.White, 3))
                {
                    if (symbol == "check")
                    {
                        g.DrawLine(p, 8, 16, 14, 22);
                        g.DrawLine(p, 14, 22, 24, 8);
                    }
                    else if (symbol == "cross")
                    {
                         g.DrawLine(p, 10, 10, 22, 22);
                         g.DrawLine(p, 22, 10, 10, 22);
                    }
                    else if (symbol == "clipboard")
                    {
                         // Simple Clipboard shape
                         // Board
                         g.DrawRectangle(new Pen(Color.White, 2), 9, 8, 14, 16);
                         // Clip
                         g.FillRectangle(Brushes.White, 12, 6, 8, 4);
                    }
                }
                
                return Icon.FromHandle(bmp.GetHicon());
            }
        }
    }
}
