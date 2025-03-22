using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CodeWalker.GameFiles;
using System;
using System.Collections.Generic;
using Swashbuckle.AspNetCore.Annotations;
using CodeWalker.API.Services;

[Route("api")]
[ApiController]
public class ImportController : ControllerBase
{
    private readonly RpfService _rpfService;
    private readonly ConfigService _configService;

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

    public ImportController(RpfService rpfService, ConfigService configService)
    {
        _rpfService = rpfService;
        _configService = configService;
    }

    [HttpPost("import-xml")]
    [SwaggerOperation(
        Summary = "Imports XML files into an RPF archive",
        Description = "Reads an XML file, processes it, and imports it into an RPF archive. Uses configured output and RPF path.")]
    [SwaggerResponse(200, "File imported successfully into RPF", typeof(List<object>))]
    [SwaggerResponse(400, "Bad request due to missing parameters")]
    public async Task<IActionResult> ImportXml([
        FromForm, SwaggerParameter("List of XML file paths to import", Required = true)
    ] List<string> filePaths)
    {
        var config = _configService.Get();
        string rpfArchivePath = config.RpfArchivePath;
        string outputFolder = config.FivemOutputDir;

        Console.WriteLine("[DEBUG] Received Import Request");
        Console.WriteLine($"[DEBUG] Processing {filePaths.Count} files");
        Console.WriteLine($"[DEBUG] RPF Archive Path: {rpfArchivePath}");
        Console.WriteLine($"[DEBUG] Output Folder: {outputFolder}");

        if (filePaths == null || filePaths.Count == 0)
            return BadRequest("No files provided.");

        if (string.IsNullOrWhiteSpace(rpfArchivePath) || !System.IO.File.Exists(rpfArchivePath))
            return BadRequest("Configured RPF archive path is invalid or missing.");

        var results = new List<object>();

        foreach (var filePath in filePaths)
        {
            Console.WriteLine($"[DEBUG] Processing file: {filePath}");

            if (!System.IO.File.Exists(filePath))
            {
                results.Add(new { filePath, error = "File not found." });
                continue;
            }

            try
            {
                string xmlText = await System.IO.File.ReadAllTextAsync(filePath);
                var doc = new System.Xml.XmlDocument();
                doc.LoadXml(xmlText);

                string fullFilename = Path.GetFileNameWithoutExtension(filePath);
                var fileFormat = XmlMeta.GetXMLFormat(filePath.ToLower(), out int trimLength);

                if (!ValidMetaFormats.Contains(fileFormat))
                {
                    results.Add(new { filePath, error = "Unsupported XML format." });
                    continue;
                }

                string modelName = fullFilename.Substring(0, fullFilename.Length - trimLength);
                string textureFolder = Path.Combine(Path.GetDirectoryName(filePath) ?? "", modelName);

                if (!Directory.Exists(textureFolder))
                {
                    results.Add(new { filePath, error = "Texture folder not found." });
                    continue;
                }

                byte[] data = XmlMeta.GetData(doc, fileFormat, textureFolder);

                if (data == null)
                {
                    results.Add(new { filePath, error = "Failed to process XML data." });
                    continue;
                }

                var rpfFile = _rpfService.LoadRpf(rpfArchivePath);
                var rootDir = rpfFile.Root;
                RpfFile.CreateFile(rootDir, fullFilename, data);

                string? outputFilePath = null;
                if (!string.IsNullOrWhiteSpace(outputFolder))
                {
                    // Correct filename logic to avoid double-dot issue
                    string ext = Path.GetExtension(fullFilename); // Should be ".ytyp" or similar
                    string finalName = Path.ChangeExtension(modelName, ext);
                    outputFilePath = Path.Combine(outputFolder, finalName);

                    string tempFilePath = Path.Combine(outputFolder, modelName + ".tmp");

                    try
                    {
                        System.IO.File.WriteAllBytes(tempFilePath, data);
                        if (System.IO.File.Exists(outputFilePath))
                            System.IO.File.Delete(outputFilePath);
                        System.IO.File.Move(tempFilePath, outputFilePath);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        results.Add(new { filePath, error = "Access Denied: " + ex.Message });
                        continue;
                    }
                    catch (IOException ex)
                    {
                        results.Add(new { filePath, error = "I/O Error: " + ex.Message });
                        continue;
                    }
                }

                results.Add(new
                {
                    filePath,
                    message = "File imported successfully into RPF.",
                    filename = fullFilename,
                    rpfArchivePath,
                    outputFilePath,
                    textureFolder
                });
            }
            catch (System.Xml.XmlException ex)
            {
                results.Add(new { filePath, error = $"Invalid XML format: {ex.Message}" });
            }
            catch (FileNotFoundException ex)
            {
                results.Add(new { filePath, error = ex.Message });
            }
            catch (Exception ex)
            {
                results.Add(new { filePath, error = $"Error importing file: {ex.Message}" });
            }
        }

        return Ok(results);
    }
}