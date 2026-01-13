using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace imgany
{
    static class Program
    {
        private const int RequiredMajorVersion = 10;
        private const string RuntimeDownloadUrl = "https://dotnet.microsoft.com/download/dotnet/10.0";

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Check .NET runtime version
            if (!CheckRuntimeVersion())
            {
                return;
            }
            
            // Run context
            Application.Run(new TrayApplicationContext());
        }

        private static bool CheckRuntimeVersion()
        {
            var currentVersion = Environment.Version;
            
            if (currentVersion.Major < RequiredMajorVersion)
            {
                var result = MessageBox.Show(
                    $"此程序需要 .NET {RequiredMajorVersion}.0 Desktop Runtime 才能运行。\n\n" +
                    $"当前版本: .NET {currentVersion.Major}.{currentVersion.Minor}\n" +
                    $"所需版本: .NET {RequiredMajorVersion}.0 或更高\n\n" +
                    "是否打开下载页面？",
                    "运行时版本不足",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );

                if (result == DialogResult.Yes)
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = RuntimeDownloadUrl,
                            UseShellExecute = true
                        });
                    }
                    catch
                    {
                        MessageBox.Show(
                            $"无法打开浏览器。请手动访问以下地址下载运行时：\n\n{RuntimeDownloadUrl}",
                            "提示",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );
                    }
                }

                return false;
            }

            return true;
        }
    }
}