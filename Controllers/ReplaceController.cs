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

        var fileName = Path.GetFileName(localFilePath);
        if (string.IsNullOrWhiteSpace(fileName))
            return BadRequest("Could not determine filename from localFilePath.");

        if (string.IsNullOrWhiteSpace(rpfFilePath))
            return BadRequest("Missing rpfFilePath.");

        if (!_rpfService.TryResolveEntryPath(rpfFilePath, out var rpfFile, out var targetDir, out var _, out var resolveError))
        {
            _logger.LogError("Failed to resolve RPF path: {Error}", resolveError);
            return BadRequest("Failed to resolve RPF path: " + resolveError);
        }

        if (targetDir == null)
            return BadRequest("Resolved directory was null. Can't replace file.");

        try
        {
            var existing = targetDir.Files.FirstOrDefault(f => f.NameLower == fileName.ToLowerInvariant());
            if (existing == null)
                return NotFound($"File '{fileName}' not found in RPF.");

            byte[] data = System.IO.File.ReadAllBytes(localFilePath);
            RpfFile.CreateFile(targetDir, fileName, data);

            return Ok(new
            {
                rpfFilePath,
                replacedWith = localFilePath,
                message = "File replaced successfully.",
                newSize = data.Length
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error replacing file in RPF");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
