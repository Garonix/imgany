using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace imgany.UI
{
    public class AboutDialog : Form
    {
        private const string GitHubUrl = "https://github.com/Garonix/imgany";

        public AboutDialog()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Text = "关于 imgany";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(350, 150);
            this.ShowInTaskbar = false;

            var lblInfo = new Label
            {
                Text = $"项目主页：{GitHubUrl}",
                AutoSize = false,
                Size = new Size(320, 40),
                Location = new Point(15, 25),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var btnStar = new Button
            {
                Text = "访问",
                Size = new Size(100, 30),
                Location = new Point(70, 75),
                DialogResult = DialogResult.OK
            };
            btnStar.Click += (s, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = GitHubUrl,
                        UseShellExecute = true
                    });
                }
                catch { }
            };

            var btnCancel = new Button
            {
                Text = "取消",
                Size = new Size(100, 30),
                Location = new Point(180, 75),
                DialogResult = DialogResult.Cancel
            };

            this.Controls.Add(lblInfo);
            this.Controls.Add(btnStar);
            this.Controls.Add(btnCancel);
            this.AcceptButton = btnStar;
            this.CancelButton = btnCancel;
        }
    }
}
