using imgany.Core;
using System;
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
        private ContextMenuStrip _contextMenu;
        
        public TrayApplicationContext()
        {
            _config = new ConfigManager();
            _clipboard = new ClipboardService(_config);
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
            
            _autoSaveMenuItem = new ToolStripMenuItem("自动保存");
            _autoSaveMenuItem.CheckOnClick = true;
            _autoSaveMenuItem.Checked = _config.AutoSave;
            _autoSaveMenuItem.Click += (s, e) => {
                 _config.AutoSave = _autoSaveMenuItem.Checked; 
            };
            _contextMenu.Items.Add(_autoSaveMenuItem);
            
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
            if (_clipboard.HasImage())
            {
                string savedPath = await _clipboard.SaveImageFromClipboardAsync(path);
                if (savedPath != null)
                {
                    ShowFeedback(true);
                }
            }
            else if (_clipboard.HasImageLink(out string url))
            {
                string savedPath = await _clipboard.DownloadAndSaveImageAsync(url, path);
                if (savedPath != null)
                {
                   ShowFeedback(true);
                }
                else
                {
                    // Error Feedback: If detection worked but download failed.
                    ShowFeedback(false);
                     // Optional: Keep balloon for error only? User said "Different color feedback", implied replacement?
                     // "Paint a tray icon... use color for status feedback"
                     // I will Keep balloon for error text detail, but also show red icon.
                    _trayIcon.ShowBalloonTip(3000, "下载失败", "无法下载该图片，可能是防盗链或网络超时。", ToolTipIcon.Error);
                }
            }
            // If we swallowed but failed to save (e.g. was just text), user loses paste.
            // Acceptable trade-off for "Magic" tool.
        }
        
        public void OnClipboardUpdate()
        {
            if (_config.AutoSave && !string.IsNullOrEmpty(_config.AutoSavePath))
            {
                // Feature 3: Auto Save
               // Don't await, fire and forget
               _clipboard.SaveImageFromClipboardAsync(_config.AutoSavePath);
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
                }
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
