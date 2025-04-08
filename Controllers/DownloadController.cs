using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CodeWalker.GameFiles;
using CodeWalker.API.Services; // ✅ Needed for ConfigService
using Microsoft.Extensions.Logging;
using CodeWalker.API.Utils;

namespace CodeWalker.API.Controllers
{
    [Route("api")]
    [ApiController]
    public class DownloadController : ControllerBase
    {
        private readonly ILogger<DownloadController> _logger;
        private readonly RpfService _rpfService;
        private readonly TextureExtractor _textureExtractor;
        private readonly ConfigService _configService;

        public DownloadController(
            ILogger<DownloadController> logger,
            RpfService rpfService,
            GameFileCache gameFileCache,
            ILogger<TextureExtractor> textureLogger,
            ConfigService configService) // ✅ Injected
        {
            _logger = logger;
            _rpfService = rpfService;
            _textureExtractor = new TextureExtractor(gameFileCache, textureLogger);
            _configService = configService;
        }

        [HttpGet("download-files")]
        [SwaggerOperation(
            Summary = "Downloads and extracts files",
            Description = "Extracts textures or converts YDR files to XML before saving them."
        )]
        [SwaggerResponse(200, "Successful operation", typeof(List<object>))]
        [SwaggerResponse(400, "Bad request")]
        public IActionResult DownloadFiles(
            [FromQuery, SwaggerParameter("List of full RPF paths", Required = true)]
            string[] fullPaths,
            [FromQuery] bool xml = true,
            [FromQuery] bool textures = true
        )
        {
            if (fullPaths == null || fullPaths.Length == 0)
            {
                _logger.LogWarning("No full RPF paths provided.");
                return BadRequest("At least one full RPF path is required.");
            }

            var config = _configService.Get();
            var codewalkerOutput = config.CodewalkerOutputDir;
            var blenderOutput = config.BlenderOutputDir;

            if (string.IsNullOrWhiteSpace(codewalkerOutput))
            {
                _logger.LogWarning("Configured output folder path is empty.");
                return BadRequest("Output folder is not configured.");
            }

            Directory.CreateDirectory(codewalkerOutput);
            var results = new List<object>();

            foreach (var originalPath in fullPaths)
            {
                var fullRpfPath = PathUtils.NormalizePath(originalPath);
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
                    string objectFilePath = Path.Combine(codewalkerOutput, filename);

                    if (xml)
                    {
                        string newFilename;
                        string xmlData = MetaXml.GetXml(entry, fileBytes, out newFilename, codewalkerOutput);
                        if (string.IsNullOrEmpty(xmlData))
                        {
                            results.Add(new { fullRpfPath, error = $"XML export unavailable for {Path.GetExtension(fullRpfPath)}" });
                            continue;
                        }

                        string ext = Path.GetExtension(fullRpfPath)?.TrimStart('.') ?? "bin";
                        string xmlFilePath = Path.Combine(codewalkerOutput, $"{filenameWithoutExt}.{ext}.xml");
                        System.IO.File.WriteAllText(xmlFilePath, xmlData, Encoding.UTF8);
                        results.Add(new { fullRpfPath, message = "XML saved successfully.", xmlFilePath });
                    }

                    if (textures)
                    {
                        string textureFolder = Path.Combine(codewalkerOutput, filenameWithoutExt);
                        Directory.CreateDirectory(textureFolder);
                        string tempYdrPath = Path.Combine(Path.GetTempPath(), filename);
                        System.IO.File.WriteAllBytes(tempYdrPath, fileBytes);
                        _textureExtractor.ExtractTextures(fileBytes, entry, textureFolder);
                        System.IO.File.Delete(tempYdrPath);

                        results.Add(new { fullRpfPath, message = "Textures extracted successfully.", textureFolderPath = textureFolder });

                        // ✅ Also copy to Blender Output Dir
                        if (!string.IsNullOrWhiteSpace(blenderOutput))
                        {
                            string dest = Path.Combine(blenderOutput, filenameWithoutExt);
                            if (Directory.Exists(dest)) Directory.Delete(dest, true);
                            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                            DirUtils.CopyDirectory(textureFolder, dest);
                            results.Add(new { fullRpfPath, message = "Textures copied to Blender output.", copiedTo = dest });
                        }
                    }

                    if (!xml)
                    {
                        System.IO.File.WriteAllBytes(objectFilePath, fileBytes);
                        results.Add(new { fullRpfPath, message = "File saved successfully.", objectFilePath });
                    }
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
}
