using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CodeWalker.GameFiles;
using System;
using System.Collections.Generic;



[Route("api")]
[ApiController]
public class ImportController : ControllerBase

{
    private readonly RpfService _rpfService;

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

    public ImportController(RpfService rpfService)
    {
        _rpfService = rpfService;
    }

    [HttpPost("import-xml")]
    public async Task<IActionResult> ImportXml(
    [FromForm] List<string> filePaths,
    [FromForm] string rpfArchivePath,
    [FromForm] string? outputFolder = null) // Make outputFolder optional
    {
        Console.WriteLine($"[DEBUG] Received Import Request");
        Console.WriteLine($"[DEBUG] Processing {filePaths.Count} files");
        Console.WriteLine($"[DEBUG] RPF Archive Path: {rpfArchivePath}");
        Console.WriteLine($"[DEBUG] Output Folder: {outputFolder ?? "Not Provided (Skipping file write)"}");

        if (filePaths == null || filePaths.Count == 0)
        {
            Console.WriteLine("[ERROR] No files provided.");
            return BadRequest("No files provided.");
        }

        if (string.IsNullOrWhiteSpace(rpfArchivePath) || !System.IO.File.Exists(rpfArchivePath))
        {
            Console.WriteLine("[ERROR] Invalid or missing RPF archive path.");
            return BadRequest("Invalid or missing RPF archive path.");
        }

        var results = new List<object>();

        foreach (var filePath in filePaths)
        {
            Console.WriteLine($"[DEBUG] Processing file: {filePath}");

            if (!System.IO.File.Exists(filePath))
            {
                Console.WriteLine($"[ERROR] File not found: {filePath}");
                results.Add(new { filePath, error = "File not found." });
                continue;
            }

            try
            {
                Console.WriteLine("[DEBUG] Reading XML data...");
                string xmlText = await System.IO.File.ReadAllTextAsync(filePath);
                var doc = new System.Xml.XmlDocument();
                doc.LoadXml(xmlText);
                Console.WriteLine("[DEBUG] XML successfully loaded.");

                string fullFilename = Path.GetFileNameWithoutExtension(filePath);
                var fileFormat = XmlMeta.GetXMLFormat(filePath.ToLower(), out int trimLength);

                if (!ValidMetaFormats.Contains(fileFormat))
                {
                    Console.WriteLine("[ERROR] Unsupported XML format.");
                    results.Add(new { filePath, error = "Unsupported XML format." });
                    continue;
                }

                string modelName = fullFilename.Substring(0, fullFilename.Length - trimLength);
                string textureFolder = Path.Combine(Path.GetDirectoryName(filePath), modelName);
                Console.WriteLine($"[DEBUG] Inferred Texture Folder: {textureFolder}");

                if (!Directory.Exists(textureFolder))
                {
                    Console.WriteLine("[ERROR] Texture folder does not exist.");
                    results.Add(new { filePath, error = "Texture folder not found." });
                    continue;
                }

                Console.WriteLine("[DEBUG] Calling GetData() with inferred texture folder...");
                byte[] data = XmlMeta.GetData(doc, fileFormat, textureFolder);

                if (data == null)
                {
                    Console.WriteLine("[ERROR] Failed to process XML data.");
                    results.Add(new { filePath, error = "Failed to process XML data." });
                    continue;
                }

                Console.WriteLine($"[DEBUG] XML Data processed successfully. Data Length: {data.Length} bytes");

                Console.WriteLine("[DEBUG] Loading RPF archive...");
                var rpfFile = _rpfService.LoadRpf(rpfArchivePath);
                var rootDir = rpfFile.Root;
                Console.WriteLine("[DEBUG] RPF archive loaded successfully.");

                Console.WriteLine($"[DEBUG] Importing file {fullFilename} into RPF...");
                RpfFile.CreateFile(rootDir, fullFilename, data);
                Console.WriteLine("[DEBUG] File successfully imported into RPF.");

                // **Only write the file if outputFolder is provided**
                string? outputFilePath = null;
                if (!string.IsNullOrWhiteSpace(outputFolder))
                {
                    outputFilePath = Path.Combine(outputFolder, modelName + Path.GetExtension(fullFilename));
                    Console.WriteLine($"[DEBUG] Saving file to output folder: {outputFilePath}");

                    string tempFilePath = Path.Combine(outputFolder, modelName + ".tmp");

                    try
                    {
                        Console.WriteLine($"[DEBUG] Writing temp file: {tempFilePath}");
                        System.IO.File.WriteAllBytes(tempFilePath, data);
                        Console.WriteLine("[DEBUG] Temp file written successfully.");

                        if (System.IO.File.Exists(outputFilePath))
                        {
                            Console.WriteLine("[DEBUG] Existing file detected, attempting to delete...");
                            System.IO.File.Delete(outputFilePath);
                        }

                        System.IO.File.Move(tempFilePath, outputFilePath);
                        Console.WriteLine($"[DEBUG] Successfully moved temp file to {outputFilePath}");
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        Console.WriteLine($"[ERROR] Access Denied: {ex.Message}");
                        results.Add(new { filePath, error = "Access Denied: Ensure the process has write permissions." });
                        continue;
                    }
                    catch (IOException ex)
                    {
                        Console.WriteLine($"[ERROR] IO Exception: {ex.Message}");
                        results.Add(new { filePath, error = "I/O Error: Another process may be using the file." });
                        continue;
                    }
                }

                results.Add(new
                {
                    filePath,
                    message = "File imported successfully into RPF.",
                    filename = fullFilename,
                    rpfArchivePath,
                    outputFilePath, // Might be null if outputFolder was not provided
                    textureFolder
                });
            }
            catch (System.Xml.XmlException ex)
            {
                Console.WriteLine($"[ERROR] Invalid XML format: {ex.Message}");
                results.Add(new { filePath, error = $"Invalid XML format: {ex.Message}" });
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"[ERROR] File not found: {ex.Message}");
                results.Add(new { filePath, error = ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] General Exception: {ex.Message}");
                results.Add(new { filePath, error = $"Error importing file: {ex.Message}" });
            }
        }

        return Ok(results);
    }

}
