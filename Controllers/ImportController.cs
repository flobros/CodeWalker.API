using CodeWalker.API.Services;
using CodeWalker.GameFiles;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

[Route("api")]
[ApiController]
public class ImportController : ControllerBase
{
    private readonly ILogger<ImportController> _logger;
    private readonly RpfService _rpfService;
    private readonly ConfigService _configService;

    public ImportController(ILogger<ImportController> logger, RpfService rpfService, ConfigService configService)
    {
        _logger = logger;
        _rpfService = rpfService;
        _configService = configService;
    }

    public class ImportRequest
    {
        public List<string>? FilePaths { get; set; }
        public bool? Xml { get; set; }
        public string? RpfArchivePath { get; set; }
        public string? OutputFolder { get; set; }
    }

    [HttpPost("import")]
    [Consumes("application/json")]
    [SwaggerOperation(
        Summary = "Import files into RPF archive",
        Description = "Imports one or more raw or XML files into a specified RPF archive path. Converts XML to binary if requested."
    )]
    [SwaggerResponse(200, "Import result", typeof(List<object>))]
    [SwaggerResponse(400, "Bad request (e.g. missing file paths or RPF path)")]
    public async Task<IActionResult> ImportJson([FromBody] ImportRequest json)
    {
        return await HandleImport(json.FilePaths, json.Xml, json.RpfArchivePath, json.OutputFolder);
    }

    [HttpPost("import")]
    [Consumes("application/x-www-form-urlencoded")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> ImportForm(
        [FromForm] List<string>? filePaths,
        [FromForm] bool? xml,
        [FromForm] string? rpfArchivePath,
        [FromForm] string? outputFolder)
    {
        return await HandleImport(filePaths, xml, rpfArchivePath, outputFolder);
    }

    private async Task<IActionResult> HandleImport(
        List<string>? paths,
        bool? xml,
        string? rpfPath,
        string? outputFolder)
    {
        var config = _configService.Get();
        var isXml = xml ?? false;
        rpfPath ??= config.RpfArchivePath;
        outputFolder ??= config.FivemOutputDir;

        _logger.LogDebug("Starting import. XML: {IsXml}, RPF: {RpfPath}, Output: {OutputFolder}", isXml, rpfPath, outputFolder);

        if (paths == null || paths.Count == 0)
        {
            _logger.LogDebug("No file paths provided.");
            return BadRequest("No files provided.");
        }

        if (string.IsNullOrWhiteSpace(rpfPath))
        {
            _logger.LogDebug("Missing RPF path.");
            return BadRequest("Missing RPF archive path.");
        }

        if (!_rpfService.TryResolveEntryPath(rpfPath, out var rpfFile, out var targetDir, out _, out var err))
        {
            _logger.LogDebug("Failed to resolve RPF path: {Error}", err);
            return BadRequest($"Failed to resolve RPF path: {err}");
        }

        if (targetDir == null)
        {
            _logger.LogDebug("Target directory inside RPF was null.");
            return BadRequest("Target directory inside RPF was null.");
        }

        var results = new List<object>();

        foreach (var filePath in paths)
        {
            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogDebug("File does not exist: {Path}", filePath);
                results.Add(new { filePath, error = "File not found." });
                continue;
            }

            try
            {
                byte[] data;
                string modelName;
                string finalName;

                if (isXml)
                {
                    _logger.LogDebug("Processing XML file: {File}", filePath);

                    var xmlText = await System.IO.File.ReadAllTextAsync(filePath);
                    var doc = new System.Xml.XmlDocument();
                    doc.LoadXml(xmlText);

                    var format = XmlMeta.GetXMLFormat(filePath.ToLower(), out int trimLength);
                    if (format == MetaFormat.XML)
                    {
                        _logger.LogDebug("Unhandled XML format for file: {File}", filePath);
                        results.Add(new { filePath, error = "Unknown or unhandled XML format." });
                        continue;
                    }

                    var fullFilename = Path.GetFileNameWithoutExtension(filePath);
                    modelName = fullFilename[..^trimLength];
                    var ext = Path.GetExtension(fullFilename);
                    finalName = Path.ChangeExtension(modelName, ext);

                    var texFolder = Path.Combine(Path.GetDirectoryName(filePath) ?? "", modelName);
                    if (!Directory.Exists(texFolder)) texFolder = null;

                    data = XmlMeta.GetData(doc, format, texFolder);

                    if (data == null)
                    {
                        _logger.LogDebug("Failed to convert XML to binary for file: {File}", filePath);
                        results.Add(new { filePath, error = "Failed to convert XML." });
                        continue;
                    }

                    _logger.LogDebug("XML converted successfully. Model: {Model}, Ext: {Ext}", modelName, ext);
                }
                else
                {
                    _logger.LogDebug("Reading raw file: {File}", filePath);
                    data = await System.IO.File.ReadAllBytesAsync(filePath);
                    finalName = Path.GetFileName(filePath);
                    modelName = Path.GetFileNameWithoutExtension(filePath);
                }

                _logger.LogDebug("Creating file in RPF: {FinalName} (Size: {Size} bytes)", finalName, data.Length);
                var entry = RpfFile.CreateFile(targetDir, finalName, data);

                string? outPath = null;
                if (!string.IsNullOrWhiteSpace(outputFolder))
                {
                    outPath = Path.Combine(outputFolder, finalName);
                    var temp = Path.Combine(outputFolder, modelName + ".tmp");

                    _logger.LogDebug("Writing to temp file: {Temp}", temp);
                    await System.IO.File.WriteAllBytesAsync(temp, data);

                    if (System.IO.File.Exists(outPath))
                    {
                        _logger.LogDebug("Deleting existing file: {Out}", outPath);
                        System.IO.File.Delete(outPath);
                    }

                    System.IO.File.Move(temp, outPath);
                    _logger.LogDebug("Moved temp to final: {Out}", outPath);
                }
                else
                {
                    _logger.LogDebug("Output folder is empty, skipping write.");
                }

                results.Add(new
                {
                    filePath,
                    message = isXml ? "XML imported." : "Raw file imported.",
                    filename = finalName,
                    writtenTo = entry.Path,
                    outputFilePath = outPath
                });

                _logger.LogDebug("Import result added for: {File}", finalName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while processing file: {File}", filePath);
                results.Add(new { filePath, error = ex.Message });
            }
        }

        _logger.LogDebug("Import completed. Processed {Count} file(s).", results.Count);
        return Ok(results);
    }
}
