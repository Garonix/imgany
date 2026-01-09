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
            this.Size = new Size(450, 380);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            int padding = 15;
            int y = 20;

            // Group 1: General Settings
            var grpGeneral = new GroupBox { Text = "通用设置", Location = new Point(padding, y), Size = new Size(400, 100) };
            
            var chkStartup = new CheckBox { Text = "开机自动启动 (计划任务)", Location = new Point(20, 30), AutoSize = true };
            chkStartup.Checked = _config.StartUpOnLogon;
            chkStartup.CheckedChanged += (s, e) => _config.StartUpOnLogon = chkStartup.Checked;
            
            var lblPrefix = new Label { Text = "文件名前缀:", Location = new Point(20, 60), AutoSize = true };
            _txtPrefix = new TextBox { Location = new Point(120, 58), Width = 150 };

            grpGeneral.Controls.Add(chkStartup);
            grpGeneral.Controls.Add(lblPrefix);
            grpGeneral.Controls.Add(_txtPrefix);
            
            this.Controls.Add(grpGeneral);

            y += 110;

            // Group 2: Auto Mode
            var grpAuto = new GroupBox { Text = "自动模式", Location = new Point(padding, y), Size = new Size(400, 140) };

            _chkAutoSave = new CheckBox { Text = "启用自动模式 (监听剪贴板并自动处理)", Location = new Point(20, 30), AutoSize = true };
            _chkAutoSave.CheckedChanged += (s, e) => 
            {
                _txtAutoSavePath.Enabled = _chkAutoSave.Checked;
                _chkUpload.Enabled = _chkAutoSave.Checked; // Upload depends on Auto Mode? Or independent? Usually implies auto-upload.
            };

            var lblPath = new Label { Text = "保存路径:", Location = new Point(20, 65), AutoSize = true };
            _txtAutoSavePath = new TextBox { Location = new Point(90, 62), Width = 230 };
            var btnBrowse = new Button { Text = "...", Location = new Point(330, 61), Width = 40 };
            btnBrowse.Click += OnBrowsePath;

            _chkUpload = new CheckBox { Text = "上传到图床 (即将推出)", Location = new Point(20, 100), AutoSize = true, Enabled = false };

            grpAuto.Controls.Add(_chkAutoSave);
            grpAuto.Controls.Add(lblPath);
            grpAuto.Controls.Add(_txtAutoSavePath);
            grpAuto.Controls.Add(btnBrowse);
            grpAuto.Controls.Add(_chkUpload);

            this.Controls.Add(grpAuto);

            // Buttons
            var btnSave = new Button { Text = "保存", Location = new Point(220, 300), DialogResult = DialogResult.OK };
            btnSave.Click += OnSave;
            var btnCancel = new Button { Text = "取消", Location = new Point(310, 300), DialogResult = DialogResult.Cancel };

            this.Controls.Add(btnSave);
            this.Controls.Add(btnCancel);
        }

        private void LoadSettings()
        {
            _txtPrefix.Text = _config.FilePrefix;
            _chkAutoSave.Checked = _config.AutoSave;
            _txtAutoSavePath.Text = _config.AutoSavePath;
            
            // Logic sync
            _txtAutoSavePath.Enabled = _config.AutoSave;
            // _chkUpload.Enabled = _config.AutoSave; // If upload is strictly sub-feature. 
            // Original code: upload was disabled manually. 
            // Let's keep upload disabled for now as it is "Coming Soon".
            // _chkUpload.Checked = _config.UploadToHost; 
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
