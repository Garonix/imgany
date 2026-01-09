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
        
        // Host Controls
        private CheckBox _chkUpload;
        private ComboBox _cmbHostType;
        private TextBox _txtHostUrl;
        
        private CheckBox _chkGuest;
        private TextBox _txtEmail;
        private TextBox _txtPassword;
        
        private CheckBox _chkNotify;

        public SettingsForm(ConfigManager config)
        {
            _config = config;
            InitializeComponent();
            LoadSettings();
        }

        private void ToggleHostControls(bool enabled)
        {
            _cmbHostType.Enabled = enabled;
            _txtHostUrl.Enabled = enabled;
            
            bool guest = _chkGuest.Checked;
            _chkGuest.Enabled = enabled; 
            
            _txtEmail.Enabled = enabled && !guest;
            _txtPassword.Enabled = enabled && !guest;

            _chkNotify.Enabled = enabled;
        }

        private void InitializeComponent()
        {
            this.Text = "设置 - 剪贴板工具";
            this.Size = new Size(450, 600);
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
            };

            var lblPath = new Label { Text = "保存路径:", Location = new Point(20, 65), AutoSize = true };
            _txtAutoSavePath = new TextBox { Location = new Point(90, 62), Width = 230 };
            var btnBrowse = new Button { Text = "...", Location = new Point(330, 61), Width = 40 };
            btnBrowse.Click += OnBrowsePath;
            
            grpAuto.Controls.Add(_chkAutoSave);
            grpAuto.Controls.Add(lblPath);
            grpAuto.Controls.Add(_txtAutoSavePath);
            grpAuto.Controls.Add(btnBrowse);

            this.Controls.Add(grpAuto);

            y += 150;

            // Group 3: Image Host (图床设置)
            var grpHost = new GroupBox { Text = "图床设置 (全局生效)", Location = new Point(padding, y), Size = new Size(400, 220) };
            
            _chkUpload = new CheckBox { Text = "启用自动上传 (保存本地图片时同步上传)", Location = new Point(20, 25), AutoSize = true };
            _chkUpload.CheckedChanged += (s, e) => {
                 ToggleHostControls(_chkUpload.Checked);
            };

            var lblType = new Label { Text = "图床类型:", Location = new Point(20, 55), AutoSize = true };
            _cmbHostType = new ComboBox { Location = new Point(90, 52), Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbHostType.Items.Add("Lsky Pro (兰空图床)");
            _cmbHostType.SelectedIndex = 0; // Default

            var lblHost = new Label { Text = "图床域名:", Location = new Point(20, 85), AutoSize = true };
            _txtHostUrl = new TextBox { Location = new Point(90, 82), Width = 280, PlaceholderText = "https://example.com" };

            // Auth Section
            _chkGuest = new CheckBox { Text = "以游客身份上传 (无需账号)", Location = new Point(90, 110), AutoSize = true };
            _chkGuest.CheckedChanged += (s, e) => {
                ToggleHostControls(_chkUpload.Checked);
            };

            var lblEmail = new Label { Text = "邮箱:", Location = new Point(20, 140), AutoSize = true };
            _txtEmail = new TextBox { Location = new Point(90, 137), Width = 280, PlaceholderText = "user@example.com" };

            var lblPwd = new Label { Text = "密码:", Location = new Point(20, 170), AutoSize = true };
            _txtPassword = new TextBox { Location = new Point(90, 167), Width = 280, UseSystemPasswordChar = true };

            _chkNotify = new CheckBox { Text = "上传成功/失败时系统通知", Location = new Point(90, 195), AutoSize = true };

            grpHost.Controls.Add(_chkUpload);
            grpHost.Controls.Add(lblType);
            grpHost.Controls.Add(_cmbHostType);
            grpHost.Controls.Add(lblHost);
            grpHost.Controls.Add(_txtHostUrl);
            grpHost.Controls.Add(_chkGuest);
            grpHost.Controls.Add(lblEmail);
            grpHost.Controls.Add(_txtEmail);
            grpHost.Controls.Add(lblPwd);
            grpHost.Controls.Add(_txtPassword);
            grpHost.Controls.Add(_chkNotify);

            this.Controls.Add(grpHost);

            // Buttons (Adjust Position)
            var btnSave = new Button { Text = "保存", Location = new Point(220, 510), DialogResult = DialogResult.OK };
            btnSave.Click += OnSave;
            var btnCancel = new Button { Text = "取消", Location = new Point(310, 510), DialogResult = DialogResult.Cancel };

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

            // Host
            _chkUpload.Checked = _config.UploadToHost;
            _cmbHostType.Text = _config.UploadHostType;
            _txtHostUrl.Text = _config.UploadHostUrl;
            
            _chkGuest.Checked = _config.UploadAsGuest;
            _txtEmail.Text = _config.UploadEmail;
            _txtPassword.Text = _config.UploadPassword;
            
            _chkNotify.Checked = _config.EnableUploadNotification;
            
            // Independent toggle
            ToggleHostControls(_config.UploadToHost);
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
            _config.UploadHostType = _cmbHostType.Text;
            _config.UploadHostUrl = _txtHostUrl.Text;
            
            _config.UploadAsGuest = _chkGuest.Checked;
            _config.UploadEmail = _txtEmail.Text;
            _config.UploadPassword = _txtPassword.Text;
            
            _config.EnableUploadNotification = _chkNotify.Checked;

            this.Close();
        }
    }
}
