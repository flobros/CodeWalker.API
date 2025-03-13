using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.IO;
using System.Collections.Generic;
using CodeWalker.GameFiles;

[Route("api")]
[ApiController]
public class DownloadController : ControllerBase
{
    private readonly RpfService _rpfService;

    public DownloadController(RpfService rpfService)
    {
        _rpfService = rpfService;
    }

    [HttpGet("download-files")]
    public IActionResult DownloadFiles(
        [FromQuery] string[] filenames,  // Accepts multiple filenames
        [FromQuery] bool xml = false,
        [FromQuery] string? outputFolderPath = null) // Optional, writes files if provided
    {
        if (filenames == null || filenames.Length == 0)
        {
            return BadRequest("At least one filename is required.");
        }

        var results = new List<object>();

        foreach (var filename in filenames)
        {
            try
            {
                var extractedFile = _rpfService.ExtractFileWithEntry(filename);
                if (!extractedFile.HasValue)
                {
                    results.Add(new { filename, error = $"File '{filename}' not found." });
                    continue;
                }

                var (fileBytes, entry) = extractedFile.Value;

                if (xml)
                {
                    string newFilename;
                    string xmlData = MetaXml.GetXml(entry, fileBytes, out newFilename, outputFolderPath);

                    if (string.IsNullOrEmpty(xmlData))
                    {
                        results.Add(new { filename, error = $"XML export unavailable for {Path.GetExtension(filename)}" });
                        continue;
                    }

                    if (!string.IsNullOrEmpty(outputFolderPath))
                    {
                        Directory.CreateDirectory(outputFolderPath);
                        string xmlFilePath = Path.Combine(outputFolderPath, newFilename);
                        System.IO.File.WriteAllText(xmlFilePath, xmlData, Encoding.UTF8);
                        results.Add(new { filename, message = "XML and related files saved successfully.", xmlFilePath });
                    }
                    else
                    {
                        results.Add(new { filename, message = "XML conversion successful.", xmlData });
                    }
                    continue;
                }

                // If not XML, add result to the list instead of returning immediately
                if (!string.IsNullOrEmpty(outputFolderPath))
                {
                    Directory.CreateDirectory(outputFolderPath);
                    string outputFilePath = Path.Combine(outputFolderPath, filename);
                    System.IO.File.WriteAllBytes(outputFilePath, fileBytes);
                    results.Add(new { filename, message = "File saved successfully.", outputFilePath });
                }
                else
                {
                    results.Add(new { filename, message = "File extracted successfully.", fileSize = fileBytes.Length });
                }
            }
            catch (FileNotFoundException)
            {
                results.Add(new { filename, error = "File not found." });
            }
            catch (Exception ex)
            {
                results.Add(new { filename, error = ex.Message });
            }
        }

        return Ok(results);
    }
}
