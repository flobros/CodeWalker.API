using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.Extensions.Logging;
using System.IO;
using CodeWalker.GameFiles;
using CodeWalker.API.Services;
using CodeWalker.API.Utils;
using System;
using System.Linq;

[ApiController]
[Route("api")]
public class ReplaceController : ControllerBase
{
    private readonly RpfService _rpfService;
    private readonly ConfigService _configService;
    private readonly ILogger<ReplaceController> _logger;

    public ReplaceController(RpfService rpfService, ConfigService configService, ILogger<ReplaceController> logger)
    {
        _rpfService = rpfService;
        _configService = configService;
        _logger = logger;
    }

    public class ReplaceFileRequest
    {
        public string? RpfFilePath { get; set; }
        public string? LocalFilePath { get; set; }
    }

    [HttpPost("replace-file")]
    [Consumes("application/json")]
    [SwaggerOperation(Summary = "Replaces a file in an RPF (JSON)", Description = "Replaces the file inside an RPF using a JSON body.")]
    [SwaggerResponse(200, "File replaced successfully")]
    [SwaggerResponse(400, "Bad request")]
    [SwaggerResponse(503, "Service unavailable - GTA path not configured")]
    public IActionResult ReplaceFileJson([FromBody] ReplaceFileRequest jsonBody)
    {
        return HandleReplacement(jsonBody?.RpfFilePath, jsonBody?.LocalFilePath);
    }

    [HttpPost("replace-file")]
    [Consumes("application/x-www-form-urlencoded")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult ReplaceFileForm(
        [FromForm] string? rpfFilePath,
        [FromForm] string? localFilePath)
    {
        return HandleReplacement(rpfFilePath, localFilePath);
    }

    private IActionResult HandleReplacement(string? rpfFilePath, string? localFilePath)
    {
        if (string.IsNullOrWhiteSpace(localFilePath) || !System.IO.File.Exists(localFilePath))
            return BadRequest("Invalid or missing localFilePath.");

        if (string.IsNullOrWhiteSpace(rpfFilePath))
            return BadRequest("Invalid or missing rpfFilePath.");

        try
        {
            if (!_rpfService.TryResolveEntryPath(rpfFilePath, out var rpfFile, out var targetDir, out var fileName, out var resolveError))
            {
                _logger.LogWarning("Failed to resolve RPF path: {Error}", resolveError);
                return BadRequest($"Failed to resolve RPF path: {resolveError}");
            }

            if (targetDir == null)
            {
                _logger.LogWarning("Target directory is null.");
                return BadRequest("Target directory is null.");
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                _logger.LogWarning("File name is null or empty.");
                return BadRequest("File name is null or empty.");
            }

            var localFileBytes = System.IO.File.ReadAllBytes(localFilePath);
            var newEntry = RpfFile.CreateFile(targetDir, fileName, localFileBytes);

            _logger.LogInformation("File replaced successfully: {Path}", newEntry.Path);
            return Ok(new { message = "File replaced successfully.", path = newEntry.Path });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(503, new { 
                error = "Service unavailable", 
                message = ex.Message,
                solution = "Use /api/set-config to configure a valid GTA path"
            });
        }
    }
}
