using CodeWalker.GameFiles;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.IO; // Added for Directory.Exists

namespace CodeWalker.API.Services
{
    public class ReloadableServiceContainer
    {
        private readonly ServiceFactory _serviceFactory;
        private readonly ILogger<ReloadableServiceContainer> _logger;
        private readonly ConfigService _configService;
        
        private RpfService? _rpfService;
        private GameFileCache? _gameFileCache;
        private readonly object _lock = new object();
        private int _reloadVersion = 0;

        public ReloadableServiceContainer(
            ServiceFactory serviceFactory, 
            ILogger<ReloadableServiceContainer> logger, 
            ConfigService configService)
        {
            _serviceFactory = serviceFactory;
            _logger = logger;
            _configService = configService;
            
            // Subscribe to GTA path changes
            _configService.GtaPathChanged += OnGtaPathChanged;
        }

        private void OnGtaPathChanged(string newGtaPath)
        {
            _logger.LogInformation("[ReloadableServiceContainer] GTA path changed to: {NewPath}", newGtaPath);
            ReloadServices();
        }

        public void ReloadServices()
        {
            lock (_lock)
            {
                try
                {
                    _logger.LogInformation("[ReloadableServiceContainer] Starting service reload...");
                    
                    // Increment version to invalidate current services
                    Interlocked.Increment(ref _reloadVersion);
                    
                    // Dispose old services if they implement IDisposable
                    if (_rpfService is IDisposable disposableRpf)
                    {
                        disposableRpf.Dispose();
                    }
                    if (_gameFileCache is IDisposable disposableCache)
                    {
                        disposableCache.Dispose();
                    }
                    
                    // Clear references
                    _rpfService = null;
                    _gameFileCache = null;
                    
                    _logger.LogInformation("[ReloadableServiceContainer] Services cleared. Creating new services immediately...");
                    
                    // Immediately create new services with the updated configuration
                    var config = _configService.Get();
                    string gtaPath = config.GTAPath;
                    
                    if (!Directory.Exists(gtaPath))
                    {
                        _logger.LogError("[ReloadableServiceContainer] GTA V directory not found at {GtaPath}", gtaPath);
                        return;
                    }

                    // Load RPF decryption keys
                    try
                    {
                        _logger.LogInformation("[ReloadableServiceContainer] Loading RPF decryption keys...");
                        GTA5Keys.LoadFromPath(gtaPath);
                        _logger.LogInformation("[ReloadableServiceContainer] RPF decryption keys loaded successfully.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[ReloadableServiceContainer] Failed to load RPF keys: {Message}", ex.Message);
                        return;
                    }

                    // Create new services immediately
                    _logger.LogInformation("[ReloadableServiceContainer] Creating new RpfService...");
                    var loggerFactory = new LoggerFactory();
                    var rpfLogger = loggerFactory.CreateLogger<RpfService>();
                    _rpfService = _serviceFactory.CreateRpfService(rpfLogger);
                    
                    _logger.LogInformation("[ReloadableServiceContainer] Creating new GameFileCache...");
                    _gameFileCache = _serviceFactory.CreateGameFileCache();
                    
                    // Preheat the new services
                    _logger.LogInformation("[ReloadableServiceContainer] Preheating new services...");
                    int count = _rpfService.Preheat();
                    
                    try
                    {
                        _logger.LogInformation("[ReloadableServiceContainer] Preloading cache with known meta types...");
                        // Preload by hash
                        uint hash = JenkHash.GenHash("prop_alien_egg_01");
                        var ydr = _gameFileCache.GetYdr(hash);
                        if (ydr != null)
                            _logger.LogInformation("[ReloadableServiceContainer] YDR preloaded successfully.");
                        else
                            _logger.LogInformation("[ReloadableServiceContainer] YDR not found in archive.");

                        _logger.LogInformation("[ReloadableServiceContainer] Archetype dict contains: " + _gameFileCache.GetArchetype(hash)?.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[ReloadableServiceContainer] Cache preloading failed: {Message}", ex.Message);
                    }
                    
                    _logger.LogInformation("[ReloadableServiceContainer] Service reload completed. Preheated RPF with {Count} entries.", count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[ReloadableServiceContainer] Error during service reload: {Message}", ex.Message);
                }
            }
        }

        public RpfService GetRpfService(ILogger<RpfService> logger)
        {
            lock (_lock)
            {
                if (_rpfService == null)
                {
                    var config = _configService.Get();
                    string gtaPath = config.GTAPath;
                    
                    if (string.IsNullOrWhiteSpace(gtaPath))
                    {
                        throw new InvalidOperationException("GTA path is not configured. Please use /api/set-config to set a valid GTA path.");
                    }
                    
                    if (!Directory.Exists(gtaPath))
                    {
                        throw new InvalidOperationException($"GTA V directory not found at {gtaPath}. Please use /api/set-config to set a valid GTA path.");
                    }
                    
                    _logger.LogInformation("[ReloadableServiceContainer] Creating new RpfService...");
                    _rpfService = _serviceFactory.CreateRpfService(logger);
                }
                return _rpfService;
            }
        }

        public GameFileCache GetGameFileCache()
        {
            lock (_lock)
            {
                if (_gameFileCache == null)
                {
                    var config = _configService.Get();
                    string gtaPath = config.GTAPath;
                    
                    if (string.IsNullOrWhiteSpace(gtaPath))
                    {
                        throw new InvalidOperationException("GTA path is not configured. Please use /api/set-config to set a valid GTA path.");
                    }
                    
                    if (!Directory.Exists(gtaPath))
                    {
                        throw new InvalidOperationException($"GTA V directory not found at {gtaPath}. Please use /api/set-config to set a valid GTA path.");
                    }
                    
                    _logger.LogInformation("[ReloadableServiceContainer] Creating new GameFileCache...");
                    _gameFileCache = _serviceFactory.CreateGameFileCache();
                }
                return _gameFileCache;
            }
        }

        public int GetReloadVersion() => _reloadVersion;

        public void Dispose()
        {
            _configService.GtaPathChanged -= OnGtaPathChanged;
            
            if (_rpfService is IDisposable disposableRpf)
            {
                disposableRpf.Dispose();
            }
            if (_gameFileCache is IDisposable disposableCache)
            {
                disposableCache.Dispose();
            }
        }
    }
} 