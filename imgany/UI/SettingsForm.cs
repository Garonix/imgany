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

        // Layout Containers
        private GroupBox grpHost;
        private GroupBox grpAuto;
        private TableLayoutPanel mainLayout;

        public SettingsForm(ConfigManager config)
        {
            _config = config;
            InitializeComponent();
            LoadSettings();
        }

        private void ToggleAutoSaveControls()
        {
            grpAuto.Visible = _chkAutoSave.Checked;
            ToggleHostControls();
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
            grpHost.Visible = _chkUpload.Checked;

            bool uploadEnabled = _chkUpload.Checked;
            
            _cmbHostType.Enabled = uploadEnabled;
            _txtHostUrl.Enabled = uploadEnabled;
            
            bool guest = _chkGuest.Checked;
            _chkGuest.Enabled = uploadEnabled; 
            
            _txtEmail.Enabled = uploadEnabled && !guest;
            _txtPassword.Enabled = uploadEnabled && !guest;

            _chkNotify.Enabled = uploadEnabled;

            bool canUseUploadOnly = _chkAutoSave.Checked && uploadEnabled;
            
            _chkUploadOnly.Enabled = canUseUploadOnly; 
            if (!canUseUploadOnly) _chkUploadOnly.Checked = false; 
        }

        private void InitializeComponent()
        {
            this.Text = "设置";
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.Padding = new Padding(15);
            this.MinimumSize = new Size(480, 200); // 确保最小宽度

            // Main Layout Container
            mainLayout = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                MinimumSize = new Size(440, 0) // 内容区域最小宽度
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            // ============================================
            // Group 1: General Settings
            // ============================================
            var grpGeneral = new GroupBox 
            { 
                Text = "通用设置", 
                ForeColor = Color.Gray,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 10),
                MinimumSize = new Size(440, 0)
            };

            var generalLayout = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                ForeColor = SystemColors.ControlText
            };
            generalLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            generalLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            // Row 0: Checkboxes - use FlowLayoutPanel for horizontal alignment
            var checkboxPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                Margin = new Padding(0, 0, 0, 5),
                WrapContents = false
            };

            var chkStartup = new CheckBox 
            { 
                Text = "开机自启", 
                AutoSize = true,
                Margin = new Padding(0, 0, 25, 0)
            };
            chkStartup.Checked = _config.StartUpOnLogon;
            chkStartup.CheckedChanged += (s, e) => _config.StartUpOnLogon = chkStartup.Checked;
            
            _chkAutoSave = new CheckBox 
            { 
                Text = "自动模式", 
                AutoSize = true,
                Margin = new Padding(0, 0, 25, 0)
            };
            _chkAutoSave.CheckedChanged += (s, e) => ToggleAutoSaveControls();
            
            _chkUpload = new CheckBox 
            { 
                Text = "上传图床", 
                AutoSize = true,
                Margin = new Padding(0)
            };
            _chkUpload.CheckedChanged += (s, e) => ToggleHostControls();

            checkboxPanel.Controls.Add(chkStartup);
            checkboxPanel.Controls.Add(_chkAutoSave);
            checkboxPanel.Controls.Add(_chkUpload);

            generalLayout.Controls.Add(checkboxPanel, 0, 0);
            generalLayout.SetColumnSpan(checkboxPanel, 2);

            // Row 1: Prefix - aligned row
            var lblPrefix = new Label 
            { 
                Text = "文件名前缀:", 
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 0, 10, 0)
            };
            _txtPrefix = new TextBox 
            { 
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 3, 0, 3)
            };

            generalLayout.Controls.Add(lblPrefix, 0, 1);
            generalLayout.Controls.Add(_txtPrefix, 1, 1);

            grpGeneral.Controls.Add(generalLayout);
            mainLayout.Controls.Add(grpGeneral, 0, 0);

            // ============================================
            // Group 2: Auto Mode Settings
            // ============================================
            grpAuto = new GroupBox 
            { 
                Text = "自动模式设置", 
                ForeColor = Color.Gray,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 10),
                MinimumSize = new Size(440, 0)
            };

            var autoLayout = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 3,
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                ForeColor = SystemColors.ControlText
            };
            autoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            autoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            autoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            // Row 0: Favorite Paths
            var lblFav = new Label 
            { 
                Text = "常用路径:", 
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 0, 10, 0)
            };
            _cmbFavoritePaths = new ComboBox 
            { 
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 3, 10, 3)
            };
            _cmbFavoritePaths.Items.Add("-- 请选择 --");
            _cmbFavoritePaths.SelectedIndex = 0;
            _cmbFavoritePaths.SelectedIndexChanged += OnFavoritePathSelected;
            
            _btnManage = new Button 
            { 
                Text = "管理", 
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(70, 0),
                Margin = new Padding(0, 3, 0, 3)
            };
            _btnManage.Click += OnManagePaths;

            autoLayout.Controls.Add(lblFav, 0, 0);
            autoLayout.Controls.Add(_cmbFavoritePaths, 1, 0);
            autoLayout.Controls.Add(_btnManage, 2, 0);

            // Row 1: Save Path
            var lblPath = new Label 
            { 
                Text = "保存路径:", 
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 0, 10, 0)
            };
            _txtAutoSavePath = new TextBox 
            { 
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 3, 10, 3)
            };
            _btnBrowse = new Button 
            { 
                Text = "浏览", 
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(70, 0),
                Margin = new Padding(0, 3, 0, 3)
            };
            _btnBrowse.Click += OnBrowsePath;
            
            autoLayout.Controls.Add(lblPath, 0, 1);
            autoLayout.Controls.Add(_txtAutoSavePath, 1, 1);
            autoLayout.Controls.Add(_btnBrowse, 2, 1);

            grpAuto.Controls.Add(autoLayout);
            mainLayout.Controls.Add(grpAuto, 0, 1);

            // ============================================
            // Group 3: Image Host Settings
            // ============================================
            grpHost = new GroupBox 
            { 
                Text = "图床设置", 
                ForeColor = Color.Gray,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 10),
                MinimumSize = new Size(440, 0)
            };

            var hostLayout = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                ForeColor = SystemColors.ControlText
            };
            hostLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            hostLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            // Row 0: Upload Options - use FlowLayoutPanel
            var uploadOptionsPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                Margin = new Padding(0, 0, 0, 5),
                WrapContents = false
            };

            _chkUploadOnly = new CheckBox 
            { 
                Text = "仅上传模式 (不保存本地)", 
                AutoSize = true,
                Margin = new Padding(0, 0, 25, 0)
            };
            _chkNotify = new CheckBox 
            { 
                Text = "上传结果通知", 
                AutoSize = true,
                Margin = new Padding(0)
            };

            uploadOptionsPanel.Controls.Add(_chkUploadOnly);
            uploadOptionsPanel.Controls.Add(_chkNotify);

            hostLayout.Controls.Add(uploadOptionsPanel, 0, 0);
            hostLayout.SetColumnSpan(uploadOptionsPanel, 2);

            // Row 1: Host Type
            var lblType = new Label 
            { 
                Text = "图床类型:", 
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 0, 10, 0)
            };
            _cmbHostType = new ComboBox 
            { 
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 3, 0, 3)
            };
            _cmbHostType.Items.Add("Lsky Pro (兰空图床)");
            _cmbHostType.SelectedIndex = 0;
            
            hostLayout.Controls.Add(lblType, 0, 1);
            hostLayout.Controls.Add(_cmbHostType, 1, 1);

            // Row 2: Host URL
            var lblHost = new Label 
            { 
                Text = "图床域名:", 
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 0, 10, 0)
            };
            _txtHostUrl = new TextBox 
            { 
                Dock = DockStyle.Fill,
                PlaceholderText = "https://example.com",
                Margin = new Padding(0, 3, 0, 3)
            };

            hostLayout.Controls.Add(lblHost, 0, 2);
            hostLayout.Controls.Add(_txtHostUrl, 1, 2);

            // Row 3: Guest Mode
            _chkGuest = new CheckBox 
            { 
                Text = "游客上传(需服务端支持)", 
                AutoSize = true,
                Margin = new Padding(0, 5, 0, 5)
            };
            _chkGuest.CheckedChanged += (s, e) => ToggleHostControls();

            hostLayout.Controls.Add(_chkGuest, 1, 3);

            // Row 4: Email
            var lblEmail = new Label 
            { 
                Text = "邮箱:", 
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 0, 10, 0)
            };
            _txtEmail = new TextBox 
            { 
                Dock = DockStyle.Fill,
                PlaceholderText = "user@example.com",
                Margin = new Padding(0, 3, 0, 3)
            };

            hostLayout.Controls.Add(lblEmail, 0, 4);
            hostLayout.Controls.Add(_txtEmail, 1, 4);

            // Row 5: Password
            var lblPwd = new Label 
            { 
                Text = "密码:", 
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 0, 10, 0)
            };
            _txtPassword = new TextBox 
            { 
                Dock = DockStyle.Fill,
                UseSystemPasswordChar = true,
                Margin = new Padding(0, 3, 0, 3)
            };

            hostLayout.Controls.Add(lblPwd, 0, 5);
            hostLayout.Controls.Add(_txtPassword, 1, 5);

            grpHost.Controls.Add(hostLayout);
            mainLayout.Controls.Add(grpHost, 0, 2);

            // ============================================
            // Buttons Panel
            // ============================================
            var buttonPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 5, 0, 0)
            };

            var btnCancel = new Button 
            { 
                Text = "取消", 
                DialogResult = DialogResult.Cancel,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(80, 0),
                Margin = new Padding(0)
            };
            var btnSave = new Button 
            { 
                Text = "保存",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(80, 0),
                Margin = new Padding(0, 0, 10, 0)
            };
            btnSave.Click += OnSave;

            buttonPanel.Controls.Add(btnCancel);
            buttonPanel.Controls.Add(btnSave);
            mainLayout.Controls.Add(buttonPanel, 0, 3);

            this.Controls.Add(mainLayout);
            this.AcceptButton = btnSave;
            this.CancelButton = btnCancel;
        }

        private void LoadSettings()
        {
            _txtPrefix.Text = _config.FilePrefix;
            _chkAutoSave.Checked = _config.AutoSave;
            _txtAutoSavePath.Text = _config.AutoSavePath;
            
            RefreshFavoritePaths();
            
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
            
            _config.EnableUploadNotification = _chkNotify.Checked;
            _config.UploadOnly = _chkUploadOnly.Checked;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
