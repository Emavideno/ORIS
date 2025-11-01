using System;
using System.IO;
using System.Text.Json;

namespace MiniHttpServer.shared
{
    public class SettingsManager
    {
        private static SettingsManager _instance;
        private static readonly object _lock = new object();

        public SettingsModel Settings { get; private set; }

        private SettingsManager()
        {
            LoadSettings();
        }

        public static SettingsManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new SettingsManager();
                    }
                }
                return _instance;
            }
        }

        private void LoadSettings()
        {
            string path = "settings.json";
            if (!File.Exists(path))
                throw new FileNotFoundException("Файл настроек не найден", path);

            string json = File.ReadAllText(path);
            Settings = JsonSerializer.Deserialize<SettingsModel>(json)
                       ?? throw new InvalidOperationException("Не удалось десериализовать настройки");
        }

        public string PublicDirectoryPath => Settings.PublicDirectoryPath;
        public string Domain => Settings.Domain;
        public int Port => Settings.Port;

        public void ReloadSettings()
        {
            lock (_lock)
            {
                LoadSettings();
            }
        }
    }
}