using CodeWalker.API.Models;
using System.Text.Json;

namespace CodeWalker.API.Services
{
    public class ConfigService
    {
        private readonly string ConfigFilePath;
        private ApiConfig _config = new();
        private string _lastGtaPath = "";

        // Event to notify when GTA path changes
        public event Action<string>? GtaPathChanged;

        public ConfigService()
        {
            ConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "userconfig.json");
            Load(); // Load from disk on startup
            _lastGtaPath = _config.GTAPath; // Store initial GTA path
        }

        public ApiConfig Get() => _config;

        public void Set(ApiConfig config)
        {
            var oldGtaPath = _config.GTAPath;
            _config = config;
            Save();
            
            // Check if GTA path has changed
            if (!string.Equals(oldGtaPath, config.GTAPath, StringComparison.OrdinalIgnoreCase))
            {
                _lastGtaPath = config.GTAPath;
                GtaPathChanged?.Invoke(config.GTAPath);
            }
        }

        public bool HasGtaPathChanged()
        {
            return !string.Equals(_lastGtaPath, _config.GTAPath, StringComparison.OrdinalIgnoreCase);
        }

        public string GetCurrentGtaPath() => _config.GTAPath;

        private void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(ConfigFilePath);
                if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigFilePath, json);
                Console.WriteLine($"[CONFIG] ✅ Saved to {ConfigFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CONFIG] ❌ Failed to save config: {ex.Message}");
            }
        }

        private void Load()
        {
            try
            {
                Console.WriteLine($"[CONFIG] Looking for config at: {Path.GetFullPath(ConfigFilePath)}");

                if (!File.Exists(ConfigFilePath))
                {
                    Console.WriteLine($"[CONFIG] ❌ File missing: {ConfigFilePath}");
                    return;
                }

                var json = File.ReadAllText(ConfigFilePath);
                Console.WriteLine($"[CONFIG] Raw JSON: {json}");

                var loadedConfig = JsonSerializer.Deserialize<ApiConfig>(json);
                if (loadedConfig == null)
                {
                    Console.WriteLine($"[CONFIG] ❌ Failed to parse config.");
                    return;
                }

                _config = loadedConfig;
                Console.WriteLine($"[CONFIG] ✅ Loaded config from {ConfigFilePath}");
                Console.WriteLine($"[CONFIG] GTAPath = {_config.GTAPath}");

                if (string.IsNullOrWhiteSpace(_config.GTAPath))
                {
                    Console.Error.WriteLine($"[CONFIG] ❌ GTAPath is null or empty. Cannot continue.");
                    Environment.Exit(1); // Hard stop
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CONFIG] ❌ Failed to load config: {ex.Message}");
            }
        }

    }
}
