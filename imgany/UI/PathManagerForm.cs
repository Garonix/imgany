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
            // Clone dictionary to avoid direct modification until Save
            _paths = new Dictionary<string, string>(_config.FavoritePaths);
            InitializeComponent();
            RefreshList();
        }

        private void InitializeComponent()
        {
            this.Text = "管理自动保存路径";
            this.Size = new Size(400, 300); // Further Reduced size
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            int padding = 10;
            int y = 10;
            int grpW = 360;

            // Input Area
            var grpInput = new GroupBox { Text = "新增/修改", Location = new Point(padding, y), Size = new Size(grpW, 100) };
            
            int row1 = 25;
            int row2 = 60;
            int inputX = 55;
            int btnW_Add = 60; // Standardized
            int btnW_Browse = 60; // Standardized
            int rightMargin = grpW - 10;

            // Row 1: Alias
            var lblAlias = new Label { Text = "别名:", Location = new Point(10, row1+3), AutoSize = true };
            
            // Align Add button to right
            int btnAddX = rightMargin - btnW_Add;
            var btnAdd = new Button { Text = "添加", Location = new Point(btnAddX, row1-1), Width = btnW_Add, Height = 25 };
            btnAdd.Click += OnAdd;

            // Alias Txt fills space
            int aliasW = btnAddX - 10 - inputX;
            _txtAlias = new TextBox { Location = new Point(inputX, row1), Width = aliasW, PlaceholderText = "例如: 工作目录" };
            
            // Row 2: Path
            var lblPath = new Label { Text = "路径:", Location = new Point(10, row2+3), AutoSize = true };
            
            // Align Browse button to right (same right edge as Add)
            int btnBrowseX = rightMargin - btnW_Browse;
            var btnBrowse = new Button { Text = "浏览", Location = new Point(btnBrowseX, row2-1), Width = btnW_Browse, Height = 25 };
            btnBrowse.Click += (s, e) => 
            {
                using (var fbd = new FolderBrowserDialog())
                {
                    if (fbd.ShowDialog() == DialogResult.OK) _txtPath.Text = fbd.SelectedPath;
                }
            };
            
            // Path Txt fills space (Same width as Alias)
            int pathW = btnBrowseX - 10 - inputX;
            _txtPath = new TextBox { Location = new Point(inputX, row2), Width = pathW, PlaceholderText = "D:\\Images" };
            
            grpInput.Controls.Add(lblAlias);
            grpInput.Controls.Add(_txtAlias);
            grpInput.Controls.Add(lblPath);
            grpInput.Controls.Add(_txtPath);
            grpInput.Controls.Add(btnBrowse);
            grpInput.Controls.Add(btnAdd);

            this.Controls.Add(grpInput);
            
            y += 105;

            // List Area
            _lstPaths = new ListBox { Location = new Point(padding, y), Size = new Size(grpW, 105) };
            _lstPaths.SelectedIndexChanged += OnSelectionChanged;
            this.Controls.Add(_lstPaths);

            y += 115;

            // Buttons (Bottom) using Anchor logic mostly or absolute
            // Remove: Left (Field)
            _btnRemove = new Button { Text = "删除", Location = new Point(padding, y), Width = 70, Enabled = false };
            _btnRemove.Click += OnRemove;
            
            // Save/Cancel: Right
            int btnActionW = 70;
            int btnCancelX = padding + grpW - btnActionW;
            int btnSaveX = btnCancelX - 10 - btnActionW;

            var btnSave = new Button { Text = "保存", Location = new Point(btnSaveX, y), DialogResult = DialogResult.OK, Width = btnActionW };
            btnSave.Click += OnSave;
            
            var btnCancel = new Button { Text = "取消", Location = new Point(btnCancelX, y), DialogResult = DialogResult.Cancel, Width = btnActionW };

            this.Controls.Add(_btnRemove);
            this.Controls.Add(btnSave);
            this.Controls.Add(btnCancel);
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
            
            // Clear inputs
            _txtAlias.Text = "";
            _txtPath.Text = "";
        }

        private void OnRemove(object sender, EventArgs e)
        {
            if (_lstPaths.SelectedIndex < 0) return;

            // Parse alias from string "Alias -> Path"
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
                   // Simple parse assuming separator format logic
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
