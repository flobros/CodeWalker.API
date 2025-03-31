using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.Extensions.Logging;
using System.IO;
using CodeWalker.GameFiles;
using CodeWalker.API.Services;
using System;

[ApiController]
[Route("api")]
public class ReplaceController : ControllerBase
{
    private readonly RpfService _rpfService;
    private readonly ConfigService _configService;
    private readonly ILogger<ReplaceController> _logger;

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').Trim();
    }

    public ReplaceController(
        RpfService rpfService,
        ConfigService configService,
        ILogger<ReplaceController> logger)
    {
        _rpfService = rpfService;
        _configService = configService;
        _logger = logger;
    }

    [HttpPost("replace-file")]
    [SwaggerOperation(
        Summary = "Replaces a file inside a given RPF archive",
        Description = "Loads the given RPF file from disk and replaces the matching internal file with a new local file. Automatically resolves relative RPF paths using the configured GTA directory.")]
    [SwaggerResponse(200, "File replaced successfully")]
    [SwaggerResponse(400, "Bad request")]
    [SwaggerResponse(404, "File not found in RPF")]
    public IActionResult ReplaceFile(
        [FromQuery, SwaggerParameter("Path to the RPF archive (relative or absolute)", Required = true)]
        string rpfFilePath,
        [FromQuery, SwaggerParameter("Absolute local path to the replacement file", Required = true)]
        string localFilePath)
    {
        if (string.IsNullOrWhiteSpace(localFilePath) || !System.IO.File.Exists(localFilePath))
            return BadRequest("Invalid or missing localFilePath.");

        if (!Path.IsPathRooted(rpfFilePath))
        {
            string basePath = _configService.Get().GTAPath;
            rpfFilePath = Path.Combine(basePath, rpfFilePath);
            _logger.LogDebug("[DEBUG] Resolved RPF path to: {ResolvedPath}", rpfFilePath);
        }

        if (!System.IO.File.Exists(rpfFilePath))
            return BadRequest("RPF archive does not exist at given path.");

        try
        {
            byte[] fileData = System.IO.File.ReadAllBytes(localFilePath);
            var rpfFile = _rpfService.LoadRpf(rpfFilePath);
            var rootDir = rpfFile.Root;

            string rpfFileName = Path.GetFileName(rpfFilePath);
            string targetFileName = Path.GetFileName(localFilePath);
            string fullRpfPath = $"{rpfFileName}/{targetFileName}";

            _logger.LogDebug("[DEBUG] Searching for entry with path: {ExpectedPath}", fullRpfPath);
            _logger.LogDebug("=== Listing all entries in RPF ===");
            foreach (var entry in rpfFile.AllEntries)
            {
                if (entry is RpfFileEntry fe)
                    _logger.LogDebug(" - {Path}", fe.Path);
            }
            _logger.LogDebug("=== End of RPF entries ===");

            foreach (var entry in rpfFile.AllEntries)
            {
                if (entry is RpfFileEntry fileEntry)
                {
                    _logger.LogDebug("[DEBUG] Checking RPF entry: {EntryPath}", fileEntry.Path);

                    if (NormalizePath(fileEntry.Path) == NormalizePath(fullRpfPath))
                    {
                        _logger.LogInformation("[MATCH] Replacing {Path} inside RPF", fileEntry.Path);
                        RpfFile.CreateFile(rootDir, Path.GetFileName(fullRpfPath), fileData);

                        return Ok(new
                        {
                            rpfFilePath,
                            fullRpfPath,
                            replacedWith = localFilePath,
                            message = "File replaced successfully.",
                            newSize = fileData.Length
                        });
                    }
                }
            }

            _logger.LogWarning("[MISS] No matching entry found for {FullRpfPath}", fullRpfPath);
            return NotFound($"File '{fullRpfPath}' not found in RPF.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error replacing file in RPF");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
