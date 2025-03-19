using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CodeWalker.GameFiles;

[Route("api")]
[ApiController]
public class DownloadController : ControllerBase
{
    private readonly ILogger<DownloadController> _logger; // ✅ Inject Logger
    private readonly RpfService _rpfService;
    private readonly TextureExtractor _textureExtractor;

    public DownloadController(ILogger<DownloadController> logger, 
                          RpfService rpfService, 
                          GameFileCache gameFileCache, 
                          ILogger<TextureExtractor> textureLogger)
{
    _logger = logger;
    _rpfService = rpfService;
    _textureExtractor = new TextureExtractor(gameFileCache, textureLogger); // ✅ Pass textureLogger
}


    [HttpGet("download-files")]
    public IActionResult DownloadFiles(
        [FromQuery] string[] filenames,
        [FromQuery] bool xml = false,
        [FromQuery] bool textures = false,
        [FromQuery] string? outputFolderPath = null)
    {
        if (filenames == null || filenames.Length == 0)
        {
            _logger.LogWarning("No filenames provided.");
            return BadRequest("At least one filename is required.");
        }

        if (string.IsNullOrEmpty(outputFolderPath))
        {
            _logger.LogWarning("No output folder path provided.");
            return BadRequest("An output folder path is required.");
        }

        Directory.CreateDirectory(outputFolderPath);
        var results = new List<object>();

        foreach (var filename in filenames)
        {
            try
            {
                var extractedFile = _rpfService.ExtractFileWithEntry(filename);
                if (!extractedFile.HasValue)
                {
                    _logger.LogWarning($"File '{filename}' not found.");
                    results.Add(new { filename, error = $"File '{filename}' not found." });
                    continue;
                }

                var (fileBytes, entry) = extractedFile.Value;
                string filenameWithoutExt = Path.GetFileNameWithoutExtension(filename);
                string objectFilePath = Path.Combine(outputFolderPath, filename);

                _logger.LogInformation($"Processing file: {filename}");

                // ✅ Handle XML Export
                if (xml)
                {
                    string newFilename;
                    string xmlData = MetaXml.GetXml(entry, fileBytes, out newFilename, outputFolderPath);
                    if (string.IsNullOrEmpty(xmlData))
                    {
                        _logger.LogWarning($"XML export unavailable for {Path.GetExtension(filename)}");
                        results.Add(new { filename, error = $"XML export unavailable for {Path.GetExtension(filename)}" });
                        continue;
                    }

                    string xmlFilePath = Path.Combine(outputFolderPath, $"{filenameWithoutExt}.ydr.xml");
                    System.IO.File.WriteAllText(xmlFilePath, xmlData, Encoding.UTF8);
                    results.Add(new { filename, message = "XML saved successfully.", xmlFilePath });
                }

                // ✅ Handle Texture Extraction
                if (textures)
                {
                    string textureFolderPath = Path.Combine(outputFolderPath, filenameWithoutExt);
                    Directory.CreateDirectory(textureFolderPath);
                    string tempYdrPath = Path.Combine(Path.GetTempPath(), filename);
                    System.IO.File.WriteAllBytes(tempYdrPath, fileBytes);
                    _textureExtractor.ExtractTextures(fileBytes, entry, textureFolderPath);
                    System.IO.File.Delete(tempYdrPath);
                    results.Add(new { filename, message = "Textures extracted successfully.", textureFolderPath });
                }

                // ✅ Save raw file **ONLY IF XML IS FALSE**
                if (!xml)
                {
                    System.IO.File.WriteAllBytes(objectFilePath, fileBytes);
                    results.Add(new { filename, message = "File saved successfully.", objectFilePath });
                }

                _logger.LogInformation($"Successfully processed file: {filename}");
            }
            catch (FileNotFoundException)
            {
                _logger.LogError($"File not found: {filename}");
                results.Add(new { filename, error = "File not found." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing {filename}: {ex.Message}");
                results.Add(new { filename, error = ex.Message });
            }
        }

        return Ok(results);
    }
}
