using imgany.Core;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace imgany.UI
{
    public class SettingsForm : Form
    {
        private ConfigManager _config;
        private TextBox _txtPrefix;
        private TextBox _txtAutoSavePath;
        private CheckBox _chkAutoSave;
        private CheckBox _chkUpload;

        public SettingsForm(ConfigManager config)
        {
            _config = config;
            InitializeComponent();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            this.Text = "设置 - 剪贴板工具";
            this.Size = new Size(400, 300);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            int padding = 20;
            int y = padding;

            // Prefix
            var lblPrefix = new Label { Text = "文件名前缀:", Location = new Point(padding, y), AutoSize = true };
            _txtPrefix = new TextBox { Location = new Point(140, y), Width = 200 };
            this.Controls.Add(lblPrefix);
            this.Controls.Add(_txtPrefix);

            y += 40;

            // Auto Save
            _chkAutoSave = new CheckBox { Text = "启用自动保存", Location = new Point(padding, y), AutoSize = true };
            _chkAutoSave.CheckedChanged += (s, e) => _txtAutoSavePath.Enabled = _chkAutoSave.Checked;
            this.Controls.Add(_chkAutoSave);

            // Startup
            var chkStartup = new CheckBox { Text = "开机自动启动 (计划任务)", Location = new Point(200, y), AutoSize = true };
            chkStartup.Checked = _config.StartUpOnLogon;
            chkStartup.CheckedChanged += (s, e) => _config.StartUpOnLogon = chkStartup.Checked;
            this.Controls.Add(chkStartup);

            y += 30;
            
            var lblPath = new Label { Text = "自动保存路径:", Location = new Point(padding, y), AutoSize = true };
            _txtAutoSavePath = new TextBox { Location = new Point(140, y), Width = 160 };
            var btnBrowse = new Button { Text = "...", Location = new Point(310, y - 1), Width = 30 };
            btnBrowse.Click += OnBrowsePath;
            
            this.Controls.Add(lblPath);
            this.Controls.Add(_txtAutoSavePath);
            this.Controls.Add(btnBrowse);

            y += 40;

            // Upload (Placeholder)
            _chkUpload = new CheckBox { Text = "上传到图床 (即将推出)", Location = new Point(padding, y), AutoSize = true, Enabled = false };
             this.Controls.Add(_chkUpload);

            // Buttons
            var btnSave = new Button { Text = "保存", Location = new Point(200, 220), DialogResult = DialogResult.OK };
            btnSave.Click += OnSave;
            var btnCancel = new Button { Text = "取消", Location = new Point(290, 220), DialogResult = DialogResult.Cancel };

            this.Controls.Add(btnSave);
            this.Controls.Add(btnCancel);
        }

        private void LoadSettings()
        {
            _txtPrefix.Text = _config.FilePrefix;
            _chkAutoSave.Checked = _config.AutoSave;
            _txtAutoSavePath.Text = _config.AutoSavePath;
            _txtAutoSavePath.Enabled = _config.AutoSave;
            _chkUpload.Checked = _config.UploadToHost;
        }

        private void OnBrowsePath(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                if (Directory.Exists(_txtAutoSavePath.Text)) fbd.SelectedPath = _txtAutoSavePath.Text;
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    _txtAutoSavePath.Text = fbd.SelectedPath;
                }
            }
        }

        private void OnSave(object sender, EventArgs e)
        {
            _config.FilePrefix = _txtPrefix.Text;
            _config.AutoSave = _chkAutoSave.Checked;
            _config.AutoSavePath = _txtAutoSavePath.Text;
            _config.UploadToHost = _chkUpload.Checked;
            this.Close();
        }
    }
}
