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
        private CheckBox _chkUploadOnly; 
        
        // Favorite Paths
        private ComboBox _cmbFavoritePaths;
        private Button _btnManage;
        private Button _btnBrowse;

        public SettingsForm(ConfigManager config)
        {
            _config = config;
            InitializeComponent();
            LoadSettings();
        }

        private void ToggleAutoSaveControls()
        {
            grpAuto.Visible = _chkAutoSave.Checked;
            ToggleHostControls(); // Cascade update
        }

        private void RefreshFavoritePaths()
        {
            _cmbFavoritePaths.Items.Clear();
            _cmbFavoritePaths.Items.Add("-- 请选择 --");
            
            foreach (var kvp in _config.FavoritePaths)
            {
                _cmbFavoritePaths.Items.Add(kvp.Key);
            }
            _cmbFavoritePaths.SelectedIndex = 0;
        }

        private void OnManagePaths(object sender, EventArgs e)
        {
            using (var form = new PathManagerForm(_config))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    RefreshFavoritePaths();
                }
            }
        }

        private void OnFavoritePathSelected(object sender, EventArgs e)
        {
            if (_cmbFavoritePaths.SelectedIndex > 0)
            {
                string alias = _cmbFavoritePaths.SelectedItem.ToString();
                if (_config.FavoritePaths.TryGetValue(alias, out string path))
                {
                    _txtAutoSavePath.Text = path;
                }
            }
        }

        private void ToggleHostControls()
        {
            // Logic: Host Group Visibility depends on Upload Checkbox
            grpHost.Visible = _chkUpload.Checked;

            // Responsive Layout: Calculate positions based on visibility
            int padding = 15;
            int baseY = 120; // After General group (y=20 + height=90 + gap=10)
            
            int currentY = baseY;
            
            // Auto group positioning
            if (_chkAutoSave.Checked)
            {
                grpAuto.Location = new Point(padding, currentY);
                currentY += 115; // grpAuto height (95) + gap (20)
            }
            
            // Host group positioning (if visible)
            if (_chkUpload.Checked)
            {
                grpHost.Location = new Point(padding, currentY);
                currentY += 220; // grpHost height (200) + gap (20)
            }
            
            // Form height = currentY + buttons area (80 = 20px gap + 60px buttons)
            this.Size = new Size(450, currentY + 80);

            bool uploadEnabled = _chkUpload.Checked;
            
            _cmbHostType.Enabled = uploadEnabled;
            _txtHostUrl.Enabled = uploadEnabled;
            
            bool guest = _chkGuest.Checked;
            _chkGuest.Enabled = uploadEnabled; 
            
            _txtEmail.Enabled = uploadEnabled && !guest;
            _txtPassword.Enabled = uploadEnabled && !guest;

            _chkNotify.Enabled = uploadEnabled;

            // Logic: UploadOnly requires BOTH "Auto Save" AND "Upload" to be enabled.
            bool canUseUploadOnly = _chkAutoSave.Checked && uploadEnabled;
            
            _chkUploadOnly.Enabled = canUseUploadOnly; 
            if (!canUseUploadOnly) _chkUploadOnly.Checked = false; 
        }

        private GroupBox grpHost; // Make class level for visibility toggle
        private GroupBox grpAuto; // Visibility controlled by ToggleAutoSaveControls

        private void InitializeComponent()
        {
            this.Text = "设置";
            this.Size = new Size(450, 600); // Height increased
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            int padding = 15;
            int y = 20;

            // Group 1: General Settings
            var grpGeneral = new GroupBox { Text = "通用设置", Location = new Point(padding, y), Size = new Size(400, 90) };
            
            // Row 1: Startup (Left) | Auto Mode (Center) | Upload (Right)
            var chkStartup = new CheckBox { Text = "开机自启", Location = new Point(20, 25), AutoSize = true };
            chkStartup.Checked = _config.StartUpOnLogon;
            chkStartup.CheckedChanged += (s, e) => _config.StartUpOnLogon = chkStartup.Checked;
            
            _chkAutoSave = new CheckBox { Text = "自动模式", Location = new Point(150, 25), AutoSize = true }; // Center
            _chkAutoSave.CheckedChanged += (s, e) => 
            {
                ToggleAutoSaveControls();
            };
            
            _chkUpload = new CheckBox { Text = "上传图床", Location = new Point(290, 25), AutoSize = true }; // Right
            _chkUpload.CheckedChanged += (s, e) => {
                 ToggleHostControls();
            };
            
            var lblPrefix = new Label { Text = "文件名前缀:", Location = new Point(20, 55), AutoSize = true };
            _txtPrefix = new TextBox { Location = new Point(100, 52), Width = 270 };

            grpGeneral.Controls.Add(chkStartup);
            grpGeneral.Controls.Add(_chkAutoSave);
            grpGeneral.Controls.Add(_chkUpload);
            grpGeneral.Controls.Add(lblPrefix);
            grpGeneral.Controls.Add(_txtPrefix);
            
            this.Controls.Add(grpGeneral);

            y += 100;

            // Group 2: Auto Mode Settings (No checkbox here anymore)
            grpAuto = new GroupBox { Text = "自动模式设置", Location = new Point(padding, y), Size = new Size(400, 95) };

            // Favorite Paths UI
            var lblFav = new Label { Text = "常用位置:", Location = new Point(20, 25), AutoSize = true };
            
            _cmbFavoritePaths = new ComboBox { Location = new Point(90, 22), Width = 230, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbFavoritePaths.Items.Add("-- 请选择 --");
            _cmbFavoritePaths.SelectedIndex = 0;
            _cmbFavoritePaths.SelectedIndexChanged += OnFavoritePathSelected;
            
            _btnManage = new Button { Text = "管理", Location = new Point(330, 21), Width = 40 };
            _btnManage.Click += OnManagePaths;

            var lblPath = new Label { Text = "保存路径:", Location = new Point(20, 55), AutoSize = true };
            _txtAutoSavePath = new TextBox { Location = new Point(90, 52), Width = 230 };
            _btnBrowse = new Button { Text = "浏览", Location = new Point(330, 51), Width = 40 };
            _btnBrowse.Click += OnBrowsePath;
            
            grpAuto.Controls.Add(lblFav);
            grpAuto.Controls.Add(_cmbFavoritePaths);
            grpAuto.Controls.Add(_btnManage);
            grpAuto.Controls.Add(lblPath);
            grpAuto.Controls.Add(_txtAutoSavePath);
            grpAuto.Controls.Add(_btnBrowse);

            this.Controls.Add(grpAuto);

            y += 115; // Adjusted for compact Auto group

            // Group 3: Image Host (图床设置)
            grpHost = new GroupBox { Text = "图床设置", Location = new Point(padding, y), Size = new Size(400, 200) };
            
            // Feature: Upload Only (Moved to Top)
            _chkUploadOnly = new CheckBox { Text = "仅上传模式 (不保存本地)", Location = new Point(20, 25), AutoSize = true };
            _chkNotify = new CheckBox { Text = "上传结果通知", Location = new Point(240, 25), AutoSize = true };

            var lblType = new Label { Text = "图床类型:", Location = new Point(20, 55), AutoSize = true };
            _cmbHostType = new ComboBox { Location = new Point(90, 52), Width = 280, DropDownStyle = ComboBoxStyle.DropDownList }; // Standardized width
            _cmbHostType.Items.Add("Lsky Pro (兰空图床)");
            _cmbHostType.SelectedIndex = 0; // Default
            
            // ... (rest of controls)

            var lblHost = new Label { Text = "图床域名:", Location = new Point(20, 85), AutoSize = true };
            _txtHostUrl = new TextBox { Location = new Point(90, 82), Width = 280, PlaceholderText = "https://example.com" };

            // Auth Section
            _chkGuest = new CheckBox { Text = "游客上传(需服务端支持)", Location = new Point(90, 110), AutoSize = true };
            _chkGuest.CheckedChanged += (s, e) => {
                ToggleHostControls();
            };

            var lblEmail = new Label { Text = "邮箱:", Location = new Point(20, 140), AutoSize = true };
            _txtEmail = new TextBox { Location = new Point(90, 137), Width = 280, PlaceholderText = "user@example.com" };

            var lblPwd = new Label { Text = "密码:", Location = new Point(20, 170), AutoSize = true };
            _txtPassword = new TextBox { Location = new Point(90, 167), Width = 280, UseSystemPasswordChar = true };

            grpHost.Controls.Add(_chkUploadOnly); // Added at top
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

            // Buttons (Adjust Position) - Dynamic position handled by FlowLayoutPanel or simpler anchor?
            // Since we resize form, we should anchor buttons to bottom right.
            var btnSave = new Button { Text = "保存" }; // No DialogResult here - set in OnSave after validation
             // Manual positioning relative to form bottom
             // We'll update button locations in ToggleHostControls or just anchor them.
             
             // Let's stick to absolute for now and update in Toggle if needed, OR just anchor.
             btnSave.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
             btnSave.Location = new Point(250, 520); // Initial
             
             var btnCancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel };
             btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
             btnCancel.Location = new Point(340, 520);
            
             // Override Click
             btnSave.Click += OnSave;

            this.Controls.Add(btnSave);
            this.Controls.Add(btnCancel);
        }

        private void LoadSettings()
        {
            _txtPrefix.Text = _config.FilePrefix;
            _chkAutoSave.Checked = _config.AutoSave;
            _txtAutoSavePath.Text = _config.AutoSavePath;
            
            RefreshFavoritePaths();
            
            // Logic sync
            ToggleAutoSaveControls();

            // Host
            _chkUpload.Checked = _config.UploadToHost;
            _cmbHostType.Text = _config.UploadHostType;
            _txtHostUrl.Text = _config.UploadHostUrl;
            
            _chkGuest.Checked = _config.UploadAsGuest;
            _txtEmail.Text = _config.UploadEmail;
            _txtPassword.Text = _config.UploadPassword;
            
            _chkNotify.Checked = _config.EnableUploadNotification;
            _chkUploadOnly.Checked = _config.UploadOnly;
            
            // Independent toggle
            ToggleHostControls(); 
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
            if (_chkAutoSave.Checked)
            {
                string path = _txtAutoSavePath.Text.Trim();
                if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                {
                    MessageBox.Show("启用自动模式必须配置有效的保存路径！", "设置错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            _config.FilePrefix = _txtPrefix.Text;
            _config.AutoSave = _chkAutoSave.Checked;
            _config.AutoSavePath = _txtAutoSavePath.Text;
            
            _config.UploadToHost = _chkUpload.Checked;
            _config.UploadHostType = _cmbHostType.Text;
            _config.UploadHostUrl = _txtHostUrl.Text;
            
            _config.UploadAsGuest = _chkGuest.Checked;
            _config.UploadEmail = _txtEmail.Text;
            _config.UploadPassword = _txtPassword.Text;
            
            _config.UploadEmail = _txtEmail.Text;
            _config.UploadPassword = _txtPassword.Text;
            
            _config.EnableUploadNotification = _chkNotify.Checked;
            _config.UploadOnly = _chkUploadOnly.Checked;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
