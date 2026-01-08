using System;
using System.Windows.Forms;

namespace WindowsClipboardTool
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // Run context
            Application.Run(new TrayApplicationContext());
        }
    }
}