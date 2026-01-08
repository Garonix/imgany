using System;
using System.IO;
using System.Text.Json;

namespace WindowsClipboardTool.Core
{
    public class AppConfig
    {
        public string FilePrefix { get; set; } = "Img";
        public bool AutoSave { get; set; } = false;
        public string AutoSavePath { get; set; } = "";
        public bool UploadToHost { get; set; } = false; // logic reserved
    }

    public class ConfigManager
    {
        private string _configPath;
        private AppConfig _config;

        public ConfigManager()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folder = Path.Combine(appData, "WindowsClipboardTool");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            _configPath = Path.Combine(folder, "config.json");
            Load();
        }

        public string FilePrefix 
        { 
            get => _config.FilePrefix; 
            set { _config.FilePrefix = value; Save(); }
        }

        public bool AutoSave 
        { 
            get => _config.AutoSave; 
            set { _config.AutoSave = value; Save(); }
        }
        
        public string AutoSavePath 
        { 
            get => _config.AutoSavePath; 
            set { _config.AutoSavePath = value; Save(); }
        }

        // Not stored in JSON, but checked against System
        public bool StartUpOnLogon
        {
             get => TaskSchedulerManager.IsTaskRegistered();
             set 
             {
                 if (value) TaskSchedulerManager.RegisterTask();
                 else TaskSchedulerManager.UnregisterTask();
             }
        }
        
         public bool UploadToHost 
        { 
            get => _config.UploadToHost; 
            set { _config.UploadToHost = value; Save(); }
        }

        private void Load()
        {
            if (File.Exists(_configPath))
            {
                try
                {
                    string json = File.ReadAllText(_configPath);
                    _config = JsonSerializer.Deserialize<AppConfig>(json);
                }
                catch
                {
                    _config = new AppConfig();
                }
            }
            else
            {
                _config = new AppConfig();
            }
        }

        private void Save()
        {
            try
            {
                string json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configPath, json);
            }
            catch { }
        }
    }
}
