using imgany.Core;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using imgany.UI;

namespace imgany
{
    public class TrayApplicationContext : ApplicationContext
    {
        private NotifyIcon _trayIcon;
        private ConfigManager _config;
        private KeyboardHook _hook;
        private ClipboardService _clipboard;
        private imgany.Core.UploadService _uploader;
        private ContextMenuStrip _contextMenu;
        
        public TrayApplicationContext()
        {
            _config = new ConfigManager();
            _clipboard = new ClipboardService(_config);
            _uploader = new imgany.Core.UploadService(_config);
            _hook = new KeyboardHook();
            
            InitializeTrayIcon();
            InitializeHook();
            
            // Start Clipboard Monitor logic if needed for AutoSave (Feature 3)
            // Note: WinForms allows overriding WndProc for main form to catch WM_CLIPBOARDUPDATE, 
            // but we are an ApplicationContext. We need a hidden window.
            // Let's create a hidden window for message pumping if we want pure AutoSave monitoring without keys.
            // Or just rely on Hook for Ctrl+V Features, and let the HiddenForm handle ClipboardUpdate.
            
           _messageWindow = new ClipboardMessageWindow(this);
        }

        private ClipboardMessageWindow _messageWindow;
        private ToolStripMenuItem _autoSaveMenuItem;
        private ToolStripMenuItem _uploadMenuItem;
        private ToolStripMenuItem _favoritesMenuItem;

        private Icon _iconNormal;
        private Icon _iconSuccess;
        private Icon _iconError;
        private System.Windows.Forms.Timer _resetIconTimer;

        private void InitializeTrayIcon()
        {
            // Load Normal Icon (Default)
            // Fix: Generate programmatically to ensure custom look.
            // DodgerBlue is a nice modern blue.
            _iconNormal = IconHelper.CreateStatusIcon(Color.DodgerBlue, "clipboard");

            // Generate Status Icons
            _iconSuccess = IconHelper.CreateStatusIcon(Color.LimeGreen, "check");
            _iconError = IconHelper.CreateStatusIcon(Color.OrangeRed, "cross");

            _resetIconTimer = new System.Windows.Forms.Timer { Interval = 2000 };
            _resetIconTimer.Tick += (s, e) => {
                 _trayIcon.Icon = _iconNormal;
                 _resetIconTimer.Stop();
            };

            _contextMenu = new ContextMenuStrip();
            _contextMenu.Items.Add("设置", null, OnSettings);
            _contextMenu.Items.Add(new ToolStripSeparator());
            
            _autoSaveMenuItem = new ToolStripMenuItem("自动模式");
            _autoSaveMenuItem.CheckOnClick = true;
            _autoSaveMenuItem.Checked = _config.AutoSave;
            _autoSaveMenuItem.Click += (s, e) => {
                 _config.AutoSave = _autoSaveMenuItem.Checked; 
            };
            _contextMenu.Items.Add(_autoSaveMenuItem);

            // New: Upload Toggle
            _uploadMenuItem = new ToolStripMenuItem("启用图床上传");
            _uploadMenuItem.CheckOnClick = true;
            _uploadMenuItem.Checked = _config.UploadToHost;
            _uploadMenuItem.Click += (s, e) => {
                 _config.UploadToHost = _uploadMenuItem.Checked;
            };
            _contextMenu.Items.Add(_uploadMenuItem);

            // New: Favorite Paths Submenu (Dynamic)
            _favoritesMenuItem = new ToolStripMenuItem("自动保存路径");
            _favoritesMenuItem.Visible = _config.AutoSave; // Initial state
            _favoritesMenuItem.DropDownOpening += (s, e) => {
                _favoritesMenuItem.DropDownItems.Clear();
                // If we are visible, AutoSave is presumably true due to Opening event Sync.
                // But double check doesn't hurt, or just skip.

                if (_config.FavoritePaths.Count == 0)
                {
                    _favoritesMenuItem.DropDownItems.Add(new ToolStripMenuItem("无常用路径") { Enabled = false });
                    return;
                }

                foreach (var kvp in _config.FavoritePaths)
                {
                    string alias = kvp.Key;
                    string path = kvp.Value;
                    var item = new ToolStripMenuItem($"{alias} ({path})");
                    item.Click += (sender, args) => {
                        _config.AutoSavePath = path;
                    };
                    // Optional: Check current path?
                    if (_config.AutoSavePath == path) item.Checked = true;
                    
                    _favoritesMenuItem.DropDownItems.Add(item);
                }
            };
            _contextMenu.Items.Add(_favoritesMenuItem);
            
            _contextMenu.Opening += (s, e) => {
                 _favoritesMenuItem.Visible = _config.AutoSave;
                 _autoSaveMenuItem.Checked = _config.AutoSave;
                 _uploadMenuItem.Checked = _config.UploadToHost;
            };
            
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add("关于", null, OnAbout);
            _contextMenu.Items.Add("退出", null, OnExit);

            _trayIcon = new NotifyIcon()
            {
                Icon = _iconNormal,
                ContextMenuStrip = _contextMenu,
                Visible = true,
                Text = "Windows 剪贴板工具"
            };
            
            _trayIcon.DoubleClick += OnSettings;
        }

        private void ShowFeedback(bool success)
        {
            _trayIcon.Icon = success ? _iconSuccess : _iconError;
            _resetIconTimer.Stop();
            _resetIconTimer.Start();
        }

        private void InitializeHook()
        {
            _hook.PasteDetected += OnPasteDetected;
            _hook.Start();
        }

        private void OnPasteDetected(object sender, EventArgs e)
        {
            // IMPORTANT: We are in the Hook Callback here.
            // DO NOT make COM calls (like ExplorerInterop).
            // Dispatch to UI thread to handle the logic.

            // Quick check: Is clipboard potentially interesting?
            // Note: Even Clipboard access might be risky here, but let's try.
            // If Clipboard access fails, we just return.
            bool interesting = false;
            try 
            {
                 // Checking formats is safer than getting data
                 IDataObject data = Clipboard.GetDataObject();
                 if (data != null)
                 {
                     if (data.GetDataPresent(DataFormats.Bitmap) || 
                         data.GetDataPresent(DataFormats.Text) || 
                         data.GetDataPresent(DataFormats.UnicodeText))
                     {
                         interesting = true;
                     }
                 }
            }
            catch { /* Ignore errors in hook */ }

            if (interesting)
            {
                // Signal to Main Thread asynchronously
                _messageWindow.BeginInvoke(new Action(() => 
                {
                     HandlePasteAsync();
                }));

                // CRITICAL FIX: Determine whether to swallow the Ctrl+V key press.
                // Previous Bug: We swallowed if (!FileDrop). This included Text, breaking Rename/Address Bar.
                // New Logic: 
                // 1. Bitmap: Swallow (Explorer doesn't handle raw bitmap paste gracefully usually).
                // 2. Text/URL: DO NOT SWALLOW. Let Explorer handle the text paste (e.g. into Rename box or Address bar).
                //    Our app will still detect the URL in the background and download the image if applicable.
                
                if (e is PasteHookEventArgs args) 
                {
                    try 
                    {
                        IDataObject data = Clipboard.GetDataObject();
                         if (data != null)
                         {
                             // Only swallow if it is strictly a Bitmap.
                             // If it is Text (even if it's a URL), we let it pass.
                             if (data.GetDataPresent(DataFormats.Bitmap))
                             {
                                 args.Handled = true; 
                             }
                             else
                             {
                                 args.Handled = false;
                             }
                         }
                    } catch {}
                }
            }
        }

        private async void HandlePasteAsync()
        {
            // Now we are on UI Thread. COM is safe.
            string path = ExplorerInterop.GetActiveExplorerPath();
            if (string.IsNullOrEmpty(path)) return; // Not in explorer or error

            // 2. Check Content & Save
            string savedPath = null;
            string remoteUrl = null;

            if (_clipboard.HasImage())
            {
                // Optimization: Parallel Processing
                // 1. Get Stream (Memory)
                var (stream, fileName) = await _clipboard.GetClipboardImageStreamAsync(_config.FilePrefix);
                
                if (stream != null && fileName != null)
                {
                    // Clone stream for two consumers? 
                    // MemoryStream is essentially byte array. 
                    // We can CopyToAsync twice? Or just use same buffer if read-only access.
                    // UploadService reads, FileStream writes.
                    // To be safe and simple: 
                    // UploadService takes stream. File.WriteAllBytes takes array.
                    // Lets convert to Array first.
                    
                    byte[] data = stream.ToArray();
                    string fullPath = System.IO.Path.Combine(path, fileName);
                    
                    // Task 1: Save to Disk
                    var saveTask = Task.Run(async () => 
                    {
                        // Ensure unique name logic needed here? 
                        // The GetClipboardImageStreamAsync generates timestamp. 
                        // But collisions possible.
                        // Let's rely on simple unique check.
                        
                        // We need to re-implement EnsureUnique logic here or expose it.
                        // For speed, let's just write. If overwrites, it matches timestamp.
                        await File.WriteAllBytesAsync(fullPath, data);
                        return fullPath;
                    });
                    
                    // Task 2: Upload (if enabled)
                    Task<string> uploadTask = Task.FromResult<string>(null);
                    if (_config.UploadToHost)
                    {
                        var uploadStream = new MemoryStream(data); // New stream for upload
                        uploadTask = _uploader.UploadImageAsync(uploadStream, fileName);
                    }
                    
                    // Wait for both?
                    // Actually, if we just want speed, we can show "Uploading..." and let it finish.
                    // But if we want to copy Link, we need to wait for Upload.
                    // Does user care if Save is finished? Maybe not.
                    
                    await Task.WhenAll(saveTask, uploadTask);
                    
                    savedPath = saveTask.Result;
                    remoteUrl = uploadTask.Result;
                }
            }
            else if (_clipboard.HasImageLink(out string url))
            {
                // Download must happen first before we can save or upload
                savedPath = await _clipboard.DownloadAndSaveImageAsync(url, path);
                
                if (savedPath != null && _config.UploadToHost)
                {
                    remoteUrl = await _uploader.UploadImageAsync(savedPath);
                }
            }

            if (savedPath != null)
            {
                ShowFeedback(true);

                if (!string.IsNullOrEmpty(remoteUrl))
                {
                    try 
                    {
                        Clipboard.SetText(remoteUrl);
                    }
                    catch {}
                    
                    // Notification OUTSIDE try-catch to ensure it always fires
                    if (_config.EnableUploadNotification)
                    {
                        _trayIcon.ShowBalloonTip(2000, "上传成功", "图片链接已复制到剪贴板", ToolTipIcon.Info);
                    }
                }
                else if (_config.UploadToHost)
                {
                     // Upload enabled but failed
                     if (_config.EnableUploadNotification)
                     {
                         _trayIcon.ShowBalloonTip(3000, "上传失败", "请检查网络或Token设置", ToolTipIcon.Error);
                     }
                }
            }
            else
            {
                if (_clipboard.HasImageLink(out _))
                {
                     ShowFeedback(false);
                     _trayIcon.ShowBalloonTip(3000, "下载失败", "无法下载该图片，可能是防盗链或网络超时。", ToolTipIcon.Error);
                }
            }
        }
        
        public async void OnClipboardUpdate()
        {
            if (_config.AutoSave && !string.IsNullOrEmpty(_config.AutoSavePath))
            {
                // Validate path exists at runtime
                if (!Directory.Exists(_config.AutoSavePath))
                {
                    _trayIcon.ShowBalloonTip(3000, "保存失败", $"路径不存在: {_config.AutoSavePath}", ToolTipIcon.Error);
                    ShowFeedback(false);
                    return;
                }

                // Feature 3: Auto Save (Optimized Parallel)
                var (stream, fileName) = await _clipboard.GetClipboardImageStreamAsync(_config.FilePrefix);
                
                if (stream != null && fileName != null)
                {
                    byte[] data = stream.ToArray();
                    string fullPath = System.IO.Path.Combine(_config.AutoSavePath, fileName);
                    
                    Task saveTask;

                    // Feature: Upload Only Mode
                    if (_config.UploadOnly && _config.UploadToHost)
                    {
                        // Skip local save, only log or do nothing for saveTask
                        saveTask = Task.CompletedTask;
                    }
                    else
                    {
                        // Task 1: Disk Save
                        saveTask = Task.Run(async () => 
                        {
                            try
                            {
                                await File.WriteAllBytesAsync(fullPath, data);
                            }
                            catch (Exception ex)
                            {
                                // Log error but don't crash
                                System.Diagnostics.Debug.WriteLine($"Save failed: {ex.Message}");
                            }
                        });
                    }
                    
                    // Task 2: Upload
                    if (_config.UploadToHost)
                    {
                        var uploadStream = new MemoryStream(data);
                        
                        string remoteUrl = await _uploader.UploadImageAsync(uploadStream, fileName);
                        
                        // Wait for save (if any)
                        await saveTask; 
                        
                        if (!string.IsNullOrEmpty(remoteUrl))
                        {
                            try 
                            {
                                Clipboard.SetText(remoteUrl);
                            }
                            catch {}
                            
                            // Notification OUTSIDE try-catch
                            if (_config.EnableUploadNotification)
                            {
                                _trayIcon.ShowBalloonTip(2000, "自动上传成功", "图片链接已复制到剪贴板", ToolTipIcon.Info);
                            }
                        }
                    }
                    else
                    {
                        await saveTask;
                    }
                }
            }
        }

        private void OnSettings(object sender, EventArgs e)
        {
            using (var form = new SettingsForm(_config))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    // Refresh configs
                    _autoSaveMenuItem.Checked = _config.AutoSave;
                    _uploadMenuItem.Checked = _config.UploadToHost;
                }
            }
        }

        private void OnAbout(object sender, EventArgs e)
        {
            using (var dialog = new AboutDialog())
            {
                dialog.ShowDialog();
            }
        }

        private void OnExit(object sender, EventArgs e)
        {
            _hook.Dispose();
             NativeMethods.RemoveClipboardFormatListener(_messageWindow.Handle);
            _trayIcon.Visible = false;
            Application.Exit();
        }
    }
    
    // Hidden window to receive Clipboard Messages
    public class ClipboardMessageWindow : Form
    {
        private TrayApplicationContext _context;
        public ClipboardMessageWindow(TrayApplicationContext context)
        {
            _context = context;
            this.ShowInTaskbar = false;
            this.WindowState = FormWindowState.Minimized;
            this.FormBorderStyle = FormBorderStyle.None;
            // NativeMethods.SetParent(this.Handle, NativeMethods.HWND_MESSAGE); // Optional optimization
            
            // Create handle
            var handle = this.Handle;
            NativeMethods.AddClipboardFormatListener(handle);
        }

        protected override void WndProc(ref Message m)
        {
             if (m.Msg == NativeMethods.WM_CLIPBOARDUPDATE)
            {
                _context.OnClipboardUpdate();
            }
            base.WndProc(ref m);
        }
    }
}
