using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using imgany.Core;

namespace imgany.UI
{
    public class PathManagerForm : Form
    {
        private ConfigManager _config;
        private ListBox _lstPaths;
        private TextBox _txtAlias;
        private TextBox _txtPath;
        private Button _btnRemove;
        private Dictionary<string, string> _paths;

        public PathManagerForm(ConfigManager config)
        {
            _config = config;
            _paths = new Dictionary<string, string>(_config.FavoritePaths);
            InitializeComponent();
            RefreshList();
        }

        private void InitializeComponent()
        {
            this.Text = "管理常用路径";
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.Padding = new Padding(15);
            this.MinimumSize = new Size(450, 320);

            // Main Layout Container
            var mainLayout = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                MinimumSize = new Size(410, 0)
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Input group
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // List
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Buttons

            // ============================================
            // Input Group
            // ============================================
            var grpInput = new GroupBox 
            { 
                Text = "新增/修改", 
                ForeColor = Color.Gray,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 10),
                MinimumSize = new Size(410, 0)
            };

            var inputLayout = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 3,
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                ForeColor = SystemColors.ControlText
            };
            inputLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            inputLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            inputLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            // Row 0: Alias
            var lblAlias = new Label 
            { 
                Text = "别名:", 
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 0, 10, 0)
            };
            _txtAlias = new TextBox 
            { 
                Dock = DockStyle.Fill,
                PlaceholderText = "例如: 工作目录",
                Margin = new Padding(0, 3, 10, 3)
            };
            var btnAdd = new Button 
            { 
                Text = "添加", 
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(70, 0),
                Margin = new Padding(0, 3, 0, 3)
            };
            btnAdd.Click += OnAdd;

            inputLayout.Controls.Add(lblAlias, 0, 0);
            inputLayout.Controls.Add(_txtAlias, 1, 0);
            inputLayout.Controls.Add(btnAdd, 2, 0);

            // Row 1: Path
            var lblPath = new Label 
            { 
                Text = "路径:", 
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 0, 10, 0)
            };
            _txtPath = new TextBox 
            { 
                Dock = DockStyle.Fill,
                PlaceholderText = "D:\\Images",
                Margin = new Padding(0, 3, 10, 3)
            };
            var btnBrowse = new Button 
            { 
                Text = "浏览", 
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(70, 0),
                Margin = new Padding(0, 3, 0, 3)
            };
            btnBrowse.Click += (s, e) => 
            {
                using (var fbd = new FolderBrowserDialog())
                {
                    if (fbd.ShowDialog() == DialogResult.OK) 
                        _txtPath.Text = fbd.SelectedPath;
                }
            };

            inputLayout.Controls.Add(lblPath, 0, 1);
            inputLayout.Controls.Add(_txtPath, 1, 1);
            inputLayout.Controls.Add(btnBrowse, 2, 1);

            grpInput.Controls.Add(inputLayout);
            mainLayout.Controls.Add(grpInput, 0, 0);

            // ============================================
            // List Box
            // ============================================
            _lstPaths = new ListBox 
            { 
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 10),
                Height = 120
            };
            _lstPaths.SelectedIndexChanged += OnSelectionChanged;
            mainLayout.Controls.Add(_lstPaths, 0, 1);

            // ============================================
            // Button Panel
            // ============================================
            var buttonPanel = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 4,
                Dock = DockStyle.Fill
            };
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); // Spacer
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            _btnRemove = new Button 
            { 
                Text = "删除", 
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(70, 0),
                Enabled = false,
                Margin = new Padding(0)
            };
            _btnRemove.Click += OnRemove;

            var btnSave = new Button 
            { 
                Text = "保存", 
                DialogResult = DialogResult.OK,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(70, 0),
                Margin = new Padding(0, 0, 10, 0)
            };
            btnSave.Click += OnSave;
            
            var btnCancel = new Button 
            { 
                Text = "取消", 
                DialogResult = DialogResult.Cancel,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(70, 0),
                Margin = new Padding(0)
            };

            buttonPanel.Controls.Add(_btnRemove, 0, 0);
            buttonPanel.Controls.Add(new Panel { Dock = DockStyle.Fill }, 1, 0); // Spacer
            buttonPanel.Controls.Add(btnSave, 2, 0);
            buttonPanel.Controls.Add(btnCancel, 3, 0);

            mainLayout.Controls.Add(buttonPanel, 0, 2);

            this.Controls.Add(mainLayout);
            this.AcceptButton = btnSave;
            this.CancelButton = btnCancel;
        }

        private void RefreshList()
        {
            _lstPaths.Items.Clear();
            foreach (var kvp in _paths)
            {
                _lstPaths.Items.Add($"{kvp.Key}  ->  {kvp.Value}");
            }
            if (_btnRemove != null) _btnRemove.Enabled = false;
        }

        private void OnAdd(object sender, EventArgs e)
        {
            string alias = _txtAlias.Text.Trim();
            string path = _txtPath.Text.Trim();

            if (string.IsNullOrEmpty(alias) || string.IsNullOrEmpty(path))
            {
                MessageBox.Show("别名和路径不能为空", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_paths.ContainsKey(alias))
            {
                if (MessageBox.Show($"别名 '{alias}' 已存在，要覆盖吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    return;
                }
            }

            _paths[alias] = path;
            RefreshList();
            
            _txtAlias.Text = "";
            _txtPath.Text = "";
        }

        private void OnRemove(object sender, EventArgs e)
        {
            if (_lstPaths.SelectedIndex < 0) return;

            string item = _lstPaths.SelectedItem.ToString();
            string alias = item.Split(new[] { "  ->" }, StringSplitOptions.None)[0].Trim();

            if (_paths.ContainsKey(alias))
            {
                _paths.Remove(alias);
                RefreshList();
            }
        }
        
        private void OnSelectionChanged(object sender, EventArgs e)
        {
            bool hasSelection = _lstPaths.SelectedIndex >= 0;
            if (_btnRemove != null) _btnRemove.Enabled = hasSelection;

            if (hasSelection)
            {
                string item = _lstPaths.SelectedItem.ToString();
                var parts = item.Split(new[] { "  ->" }, StringSplitOptions.None);
                if (parts.Length > 0)
                {
                    _txtAlias.Text = parts[0].Trim();
                    if (parts.Length > 1) _txtPath.Text = parts[1].Trim();
                }
            }
        }

        private void OnSave(object sender, EventArgs e)
        {
            _config.FavoritePaths = _paths;
            this.Close();
        }
    }
}
