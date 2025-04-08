using CodeWalker.API.Services;
using CodeWalker.GameFiles;
using Microsoft.AspNetCore.Mvc;

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
    public async Task<IActionResult> ImportJson([FromBody] ImportRequest json)
    {
        return await HandleImport(json.FilePaths, json.Xml, json.RpfArchivePath, json.OutputFolder);
    }

    [HttpPost("import")]
    [Consumes("application/x-www-form-urlencoded")]
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

        if (paths == null || paths.Count == 0)
            return BadRequest("No files provided.");
        if (string.IsNullOrWhiteSpace(rpfPath))
            return BadRequest("Missing RPF archive path.");
        if (!_rpfService.TryResolveEntryPath(rpfPath, out var rpfFile, out var targetDir, out _, out var err))
            return BadRequest($"Failed to resolve RPF path: {err}");
        if (targetDir == null)
            return BadRequest("Target directory inside RPF was null.");

        var results = new List<object>();
        foreach (var filePath in paths)
        {
            if (!System.IO.File.Exists(filePath))
            {
                results.Add(new { filePath, error = "File not found." });
                continue;
            }

            try
            {
                byte[] data;
                var fileName = Path.GetFileName(filePath);
                var modelName = Path.GetFileNameWithoutExtension(filePath);

                if (isXml)
                {
                    var xmlText = await System.IO.File.ReadAllTextAsync(filePath);
                    var doc = new System.Xml.XmlDocument();
                    doc.LoadXml(xmlText);
                    var format = XmlMeta.GetXMLFormat(filePath.ToLower(), out int trimLength);
                    if (!ValidMetaFormats.Contains(format))
                    {
                        results.Add(new { filePath, error = "Unsupported XML format." });
                        continue;
                    }
                    modelName = modelName[..^trimLength];
                    var texFolder = Path.Combine(Path.GetDirectoryName(filePath) ?? "", modelName);
                    if (!Directory.Exists(texFolder)) texFolder = null;
                    data = XmlMeta.GetData(doc, format, texFolder);
                    if (data == null)
                    {
                        results.Add(new { filePath, error = "Failed to convert XML." });
                        continue;
                    }
                }
                else
                {
                    data = await System.IO.File.ReadAllBytesAsync(filePath);
                }

                var entry = RpfFile.CreateFile(targetDir, fileName, data);
                string? outPath = null;

                if (!string.IsNullOrWhiteSpace(outputFolder))
                {
                    var finalName = Path.ChangeExtension(modelName, Path.GetExtension(filePath));
                    outPath = Path.Combine(outputFolder, finalName);
                    var temp = Path.Combine(outputFolder, modelName + ".tmp");
                    await System.IO.File.WriteAllBytesAsync(temp, data);
                    if (System.IO.File.Exists(outPath)) System.IO.File.Delete(outPath);
                    System.IO.File.Move(temp, outPath);
                }

                results.Add(new
                {
                    filePath,
                    message = isXml ? "XML imported." : "Raw file imported.",
                    filename = fileName,
                    writtenTo = entry.Path,
                    outputFilePath = outPath
                });
            }
            catch (Exception ex)
            {
                results.Add(new { filePath, error = ex.Message });
            }
        }

        return Ok(results);
    }

    private static readonly HashSet<MetaFormat> ValidMetaFormats = new()
    {
        MetaFormat.RSC, MetaFormat.XML, MetaFormat.PSO, MetaFormat.RBF,
        MetaFormat.AudioRel, MetaFormat.Ynd, MetaFormat.Ynv, MetaFormat.Ycd,
        MetaFormat.Ybn, MetaFormat.Ytd, MetaFormat.Ydr, MetaFormat.Ydd,
        MetaFormat.Yft, MetaFormat.Ypt, MetaFormat.Yld, MetaFormat.Yed,
        MetaFormat.Ywr, MetaFormat.Yvr, MetaFormat.Awc, MetaFormat.Fxc,
        MetaFormat.CacheFile, MetaFormat.Heightmap, MetaFormat.Ypdb,
        MetaFormat.Yfd, MetaFormat.Mrf
    };
}
