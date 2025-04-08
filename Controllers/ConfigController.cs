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

        public ConfigController(ConfigService configService)
        {
            _configService = configService;
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
        [SwaggerOperation(Summary = "Set config paths (Form)", Description = "Updates folder paths using form fields.")]
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
            _configService.Set(merged);
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
    }
}
