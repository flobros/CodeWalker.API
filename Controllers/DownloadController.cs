using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CodeWalker.GameFiles;

[Route("api")]
[ApiController]
public class DownloadController : ControllerBase
{
    private readonly ILogger<DownloadController> _logger;
    private readonly RpfService _rpfService;
    private readonly TextureExtractor _textureExtractor;

    public DownloadController(ILogger<DownloadController> logger,
                              RpfService rpfService,
                              GameFileCache gameFileCache,
                              ILogger<TextureExtractor> textureLogger)
    {
        _logger = logger;
        _rpfService = rpfService;
        _textureExtractor = new TextureExtractor(gameFileCache, textureLogger);
    }

    [HttpGet("download-files")]
    [SwaggerOperation(
        Summary = "Downloads and extracts files",
        Description = "Extracts textures or converts YDR files to XML before saving them."
    )]
    [SwaggerResponse(200, "Successful operation", typeof(List<object>))]
    [SwaggerResponse(400, "Bad request")]
    public IActionResult DownloadFiles(
        [FromQuery, SwaggerParameter("List of full RPF paths, e.g., fullPaths=dlc1.rpf\\x64\\models\\cdimages\\prop_alien_egg_01.ydr", Required = true)] string[] fullPaths,
        [FromQuery, SwaggerParameter("Convert files to XML (true/false), e.g., xml=true")] bool xml = true,
        [FromQuery, SwaggerParameter("Extract textures from models (true/false), e.g., textures=true")] bool textures = true,
        [FromQuery, SwaggerParameter("Output folder path where extracted files are saved, e.g., outputFolderPath=C:\\GTA_FILES", Required = true)] string outputFolderPath = "C:\\GTA_FILES")
    {
        if (fullPaths == null || fullPaths.Length == 0)
        {
            _logger.LogWarning("No full RPF paths provided.");
            return BadRequest("At least one full RPF path is required.");
        }

        if (string.IsNullOrEmpty(outputFolderPath))
        {
            _logger.LogWarning("No output folder path provided.");
            return BadRequest("An output folder path is required.");
        }

        Directory.CreateDirectory(outputFolderPath);
        var results = new List<object>();

        foreach (var fullRpfPath in fullPaths)
        {
            try
            {
                var extractedFile = _rpfService.ExtractFileWithEntry(fullRpfPath);
                if (!extractedFile.HasValue)
                {
                    _logger.LogWarning($"File '{fullRpfPath}' not found.");
                    results.Add(new { fullRpfPath, error = $"File '{fullRpfPath}' not found." });
                    continue;
                }

                var (fileBytes, entry) = extractedFile.Value;
                string filename = Path.GetFileName(fullRpfPath);
                string filenameWithoutExt = Path.GetFileNameWithoutExtension(fullRpfPath);
                string objectFilePath = Path.Combine(outputFolderPath, filename);

                _logger.LogInformation($"Processing file: {fullRpfPath}");

                if (xml)
                {
                    string newFilename;
                    string xmlData = MetaXml.GetXml(entry, fileBytes, out newFilename, outputFolderPath);
                    if (string.IsNullOrEmpty(xmlData))
                    {
                        _logger.LogWarning($"XML export unavailable for {Path.GetExtension(fullRpfPath)}");
                        results.Add(new { fullRpfPath, error = $"XML export unavailable for {Path.GetExtension(fullRpfPath)}" });
                        continue;
                    }

                    string xmlFilePath = Path.Combine(outputFolderPath, $"{filenameWithoutExt}.ydr.xml");
                    System.IO.File.WriteAllText(xmlFilePath, xmlData, Encoding.UTF8);
                    results.Add(new { fullRpfPath, message = "XML saved successfully.", xmlFilePath });
                }

                if (textures)
                {
                    string textureFolderPath = Path.Combine(outputFolderPath, filenameWithoutExt);
                    Directory.CreateDirectory(textureFolderPath);

                    string tempYdrPath = Path.Combine(Path.GetTempPath(), filename);
                    System.IO.File.WriteAllBytes(tempYdrPath, fileBytes);
                    _textureExtractor.ExtractTextures(fileBytes, entry, textureFolderPath);
                    System.IO.File.Delete(tempYdrPath);

                    results.Add(new { fullRpfPath, message = "Textures extracted successfully.", textureFolderPath });
                }

                if (!xml)
                {
                    System.IO.File.WriteAllBytes(objectFilePath, fileBytes);
                    results.Add(new { fullRpfPath, message = "File saved successfully.", objectFilePath });
                }

                _logger.LogInformation($"Successfully processed file: {fullRpfPath}");
            }
            catch (FileNotFoundException)
            {
                _logger.LogError($"File not found: {fullRpfPath}");
                results.Add(new { fullRpfPath, error = "File not found." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing {fullRpfPath}: {ex.Message}");
                results.Add(new { fullRpfPath, error = ex.Message });
            }
        }

        return Ok(results);
    }
}
