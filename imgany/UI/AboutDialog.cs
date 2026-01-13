using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace imgany.UI
{
    public class AboutDialog : Form
    {
        private const string GitHubUrl = "https://github.com/Garonix/imgany";
        
        // Standard control height for consistent alignment
        private const int ControlHeight = 25;

        public AboutDialog()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Text = "关于 imgany";
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ShowInTaskbar = false;
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.Padding = new Padding(20);
            this.MinimumSize = new Size(350, 130);

            // Main Layout Container
            var mainLayout = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                Dock = DockStyle.Fill
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Info label
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Buttons

            // Info Label
            var lblInfo = new Label
            {
                Text = $"项目主页：{GitHubUrl}",
                AutoSize = true,
                Anchor = AnchorStyles.None,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(10, 20, 10, 20)
            };
            mainLayout.Controls.Add(lblInfo, 0, 0);

            // Button Panel
            var buttonPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                Anchor = AnchorStyles.None,
                Margin = new Padding(0)
            };

            var btnStar = new Button
            {
                Text = "访问",
                Width = 80,
                Height = ControlHeight,
                DialogResult = DialogResult.OK,
                Margin = new Padding(0, 0, 10, 0)
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
                Width = 80,
                Height = ControlHeight,
                DialogResult = DialogResult.Cancel,
                Margin = new Padding(0)
            };

            buttonPanel.Controls.Add(btnStar);
            buttonPanel.Controls.Add(btnCancel);
            mainLayout.Controls.Add(buttonPanel, 0, 1);

            this.Controls.Add(mainLayout);
            this.AcceptButton = btnStar;
            this.CancelButton = btnCancel;
        }
    }
}
