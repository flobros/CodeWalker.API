using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using CodeWalker.API.Models;
using CodeWalker.API.Services;

namespace CodeWalker.API.Controllers
{
    [ApiController]
    [Route("api")]
    public class ConfigController : ControllerBase
    {
        private readonly ConfigService _configService;
        private readonly ReloadableServiceContainer _serviceContainer;

        public ConfigController(ConfigService configService, ReloadableServiceContainer serviceContainer)
        {
            _configService = configService;
            _serviceContainer = serviceContainer;
        }

        [HttpPost("set-config")]
        [Consumes("application/json")]
        [SwaggerOperation(Summary = "Set config paths (JSON)", Description = "Updates folder paths using a JSON body.")]
        public IActionResult SetConfigJson([FromBody] ApiConfig updated)
        {
            return MergeAndSaveConfig(updated);
        }

        [HttpPost("set-config")]
        [Consumes("application/x-www-form-urlencoded")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult SetConfigForm(
            [FromForm] string? CodewalkerOutputDir,
            [FromForm] string? BlenderOutputDir,
            [FromForm] string? FivemOutputDir,
            [FromForm] string? RpfArchivePath,
            [FromForm] string? GTAPath,
            [FromForm] int? Port)
        {
            var current = _configService.Get();
            var merged = new ApiConfig
            {
                CodewalkerOutputDir = CodewalkerOutputDir ?? current.CodewalkerOutputDir,
                BlenderOutputDir = BlenderOutputDir ?? current.BlenderOutputDir,
                FivemOutputDir = FivemOutputDir ?? current.FivemOutputDir,
                RpfArchivePath = RpfArchivePath ?? current.RpfArchivePath,
                GTAPath = GTAPath ?? current.GTAPath,
                Port = (Port.HasValue && Port != 0) ? Port.Value : current.Port
            };

            return MergeAndSaveConfig(merged);
        }

        private IActionResult MergeAndSaveConfig(ApiConfig merged)
        {
            var oldConfig = _configService.Get();
            var oldGtaPath = oldConfig.GTAPath;
            
            _configService.Set(merged);
            
            // Check if GTA path changed
            if (!string.Equals(oldGtaPath, merged.GTAPath, StringComparison.OrdinalIgnoreCase))
            {
                return Ok(new { 
                    message = "Configuration updated successfully. GTA path changed - services are being reloaded automatically.",
                    gtaPathChanged = true,
                    oldGtaPath = oldGtaPath,
                    newGtaPath = merged.GTAPath,
                    reloadVersion = _serviceContainer.GetReloadVersion()
                });
            }
            
            return Ok(new { message = "Configuration updated successfully." });
        }

        [HttpGet("get-config")]
        [SwaggerOperation(Summary = "Get config paths", Description = "Retrieves the currently configured folder paths.")]
        [SwaggerResponse(200, "Current config", typeof(ApiConfig))]
        public IActionResult GetConfig()
        {
            return Ok(_configService.Get());
        }

        [HttpPost("reset-config")]
        [SwaggerOperation(Summary = "Reset config paths", Description = "Resets the backend folder configuration to default values.")]
        [SwaggerResponse(200, "Configuration reset to defaults")]
        public IActionResult ResetConfig()
        {
            _configService.Set(new ApiConfig());
            return Ok(new { message = "Configuration reset to defaults." });
        }

        [HttpGet("service-status")]
        [SwaggerOperation(Summary = "Get service status", Description = "Retrieves the current service status including reload version and GTA path.")]
        [SwaggerResponse(200, "Service status")]
        public IActionResult GetServiceStatus()
        {
            var config = _configService.Get();
            var gtaPath = config.GTAPath;
            
            bool servicesReady = false;
            string statusMessage = "";
            
            if (string.IsNullOrWhiteSpace(gtaPath))
            {
                statusMessage = "GTA path is not configured";
            }
            else if (!Directory.Exists(gtaPath))
            {
                statusMessage = $"GTA V directory not found at {gtaPath}";
            }
            else
            {
                try
                {
                    // Try to get a service to see if they're working
                    var testService = _serviceContainer.GetRpfService(new LoggerFactory().CreateLogger<RpfService>());
                    servicesReady = true;
                    statusMessage = "Services are ready";
                }
                catch (InvalidOperationException ex)
                {
                    statusMessage = ex.Message;
                }
            }
            
            return Ok(new { 
                gtaPath = gtaPath,
                servicesReady = servicesReady,
                statusMessage = statusMessage,
                reloadVersion = _serviceContainer.GetReloadVersion(),
                timestamp = DateTime.UtcNow
            });
        }

        [HttpPost("reload-services")]
        [SwaggerOperation(Summary = "Reload services", Description = "Manually triggers a reload of all services (RpfService, GameFileCache, etc.) with the current configuration.")]
        [SwaggerResponse(200, "Services reloaded successfully")]
        public IActionResult ReloadServices()
        {
            var config = _configService.Get();
            var gtaPath = config.GTAPath;
            
            _serviceContainer.ReloadServices();
            
            return Ok(new { 
                message = "Services reloaded successfully.",
                gtaPath = gtaPath,
                reloadVersion = _serviceContainer.GetReloadVersion(),
                timestamp = DateTime.UtcNow
            });
        }
    }
}
