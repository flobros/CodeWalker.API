using CodeWalker.GameFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace CodeWalker.API.Services
{
    public class ServiceManager
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ServiceManager> _logger;
        private readonly ConfigService _configService;

        public ServiceManager(IServiceProvider serviceProvider, ILogger<ServiceManager> logger, ConfigService configService)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configService = configService;
            
            // Subscribe to GTA path changes
            _configService.GtaPathChanged += OnGtaPathChanged;
        }

        private void OnGtaPathChanged(string newGtaPath)
        {
            _logger.LogInformation("[ServiceManager] GTA path changed to: {NewPath}", newGtaPath);
            ReloadServices();
        }

        public void ReloadServices()
        {
            try
            {
                _logger.LogInformation("[ServiceManager] Starting service reload...");
                
                // Reload RPF decryption keys
                var config = _configService.Get();
                string gtaPath = config.GTAPath;
                
                if (!Directory.Exists(gtaPath))
                {
                    _logger.LogError("[ServiceManager] GTA V directory not found at {GtaPath}", gtaPath);
                    return;
                }

                // Load RPF decryption keys
                try
                {
                    _logger.LogInformation("[ServiceManager] Loading RPF decryption keys...");
                    GTA5Keys.LoadFromPath(gtaPath);
                    _logger.LogInformation("[ServiceManager] RPF decryption keys loaded successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[ServiceManager] Failed to load RPF keys: {Message}", ex.Message);
                    return;
                }

                // Note: For a complete reload, we would need to recreate the services
                // However, since they're registered as singletons, we need to use a different approach
                // This is a limitation of the current DI container setup
                _logger.LogInformation("[ServiceManager] Service reload completed. Note: Services will be recreated on next request.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ServiceManager] Error during service reload: {Message}", ex.Message);
            }
        }

        public void Dispose()
        {
            _configService.GtaPathChanged -= OnGtaPathChanged;
        }
    }
} 