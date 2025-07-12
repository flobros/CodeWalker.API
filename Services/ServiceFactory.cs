using CodeWalker.GameFiles;
using Microsoft.Extensions.Logging;

namespace CodeWalker.API.Services
{
    public class ServiceFactory
    {
        private readonly ILogger<ServiceFactory> _logger;
        private readonly ConfigService _configService;

        public ServiceFactory(ILogger<ServiceFactory> logger, ConfigService configService)
        {
            _logger = logger;
            _configService = configService;
        }

        public RpfService CreateRpfService(ILogger<RpfService> logger)
        {
            return new RpfService(logger, _configService);
        }

        public GameFileCache CreateGameFileCache()
        {
            var config = _configService.Get();
            string gtaPath = config.GTAPath;

            long cacheSize = 4L * 1024 * 1024 * 1024; // 4GB Cache size considering gta v enhanced
            double cacheTime = 60.0;
            bool isGen9 = config.Gen9;
            string dlc = config.Dlc;
            bool enableMods = config.EnableMods;
            string excludeFolders = "";

            var gameFileCache = new GameFileCache(cacheSize, cacheTime, gtaPath, isGen9, dlc, enableMods, excludeFolders);
            gameFileCache.EnableDlc = true; // this ensures Init() runs InitDlc()
            gameFileCache.Init(
                message => Console.WriteLine($"[GameFileCache] {message}"),
                error => Console.Error.WriteLine($"[GameFileCache ERROR] {error}")
            );
            
            _logger.LogInformation("[ServiceFactory] Created GameFileCache with GTA Path: {GtaPath}, Archetypes: {Count}", 
                gtaPath, gameFileCache.YtypDict?.Count ?? 0);
            
            return gameFileCache;
        }
    }
} 