using System;
using System.IO;
using System.Text.Json;

namespace imgany.Core
{
    public class AppConfig
    {
        public string FilePrefix { get; set; } = "Img";
        public bool AutoSave { get; set; } = false;
        public string AutoSavePath { get; set; } = "";
        
        public bool UploadToHost { get; set; } = false;

        public string UploadHostType { get; set; } = "Lsky Pro";
        public string UploadHostUrl { get; set; } = "";
        
        public bool UploadAsGuest { get; set; } = false;
        public string UploadEmail { get; set; } = ""; // Encrypted? Storage in plain json for MVP. User aware.
        public string UploadPassword { get; set; } = "";
        public string UploadToken { get; set; } = ""; // Cache
        
        public bool EnableUploadNotification { get; set; } = true;
    }

    public class ConfigManager
    {
        private string _configPath;
        private AppConfig _config;

        public ConfigManager()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folder = Path.Combine(appData, "imgany");
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

        public string UploadHostType
        {
            get => _config.UploadHostType ?? "Lsky Pro";
            set { _config.UploadHostType = value; Save(); }
        }

        public string UploadHostUrl
        {
            get => _config.UploadHostUrl ?? "";
            set { _config.UploadHostUrl = value; Save(); }
        }

        public bool UploadAsGuest
        {
            get => _config.UploadAsGuest;
            set { _config.UploadAsGuest = value; Save(); }
        }

        public string UploadEmail
        {
            get => _config.UploadEmail ?? "";
            set { _config.UploadEmail = value; Save(); }
        }

        public string UploadPassword
        {
            get => _config.UploadPassword ?? "";
            set { _config.UploadPassword = value; Save(); }
        }

        public string UploadToken
        {
            get => _config.UploadToken ?? "";
            set { _config.UploadToken = value; Save(); }
        }

        public bool EnableUploadNotification
        {
            get => _config.EnableUploadNotification;
            set { _config.EnableUploadNotification = value; Save(); }
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
