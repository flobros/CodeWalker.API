using CodeWalker.API.Models;
using System.Text.Json;

namespace CodeWalker.API.Services
{
    public class ConfigService
    {
        private readonly string ConfigFilePath;
        private ApiConfig _config = new();

        public ConfigService()
        {
            ConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "userconfig.json");
            Load(); // Load from disk on startup
        }

        public ApiConfig Get() => _config;

        public void Set(ApiConfig config)
        {
            _config = config;
            Save();
        }

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
                    Console.WriteLine($"[CONFIG] ⚠️ WARNING: GTAPath is null or empty!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CONFIG] ❌ Failed to load config: {ex.Message}");
            }
        }
    }
}
