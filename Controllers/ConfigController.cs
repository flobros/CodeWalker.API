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
        [SwaggerOperation(Summary = "Set config paths", Description = "Updates folder paths used by the backend.")]
        [SwaggerResponse(200, "Config updated")]
        public IActionResult SetConfig([FromBody] ApiConfig config)
        {
            _configService.Set(config);
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
        [SwaggerOperation(
            Summary = "Reset config paths",
            Description = "Resets the backend folder configuration to default values."
        )]
        [SwaggerResponse(200, "Configuration reset to defaults")]
        public IActionResult ResetConfig()
        {
            _configService.Set(new ApiConfig()); // Reset to defaults
            return Ok(new { message = "Configuration reset to defaults." });
        }


    }
}
