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
            ConfigFilePath = Path.Combine(AppContext.BaseDirectory, "Config", "userconfig.json");
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
                Console.WriteLine($"[CONFIG] Saved to {ConfigFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to save config: {ex.Message}");
            }
        }

        private void Load()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    var json = File.ReadAllText(ConfigFilePath);
                    var loadedConfig = JsonSerializer.Deserialize<ApiConfig>(json);
                    if (loadedConfig != null)
                    {
                        _config = loadedConfig;
                        Console.WriteLine($"[CONFIG] Loaded config from {ConfigFilePath}");
                    }
                }
                else
                {
                    Console.WriteLine("[CONFIG] No existing config found. Using defaults.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to load config: {ex.Message}");
            }
        }
    }
}
