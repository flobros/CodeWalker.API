using System;
using System.Collections.Generic;
using System.IO;
using CodeWalker.GameFiles;
using Microsoft.Extensions.Logging;

public class RpfService
{
    private readonly RpfManager _rpfManager;
    private readonly ILogger<RpfService> _logger;

    public RpfService(string gtaPath, ILogger<RpfService> logger)
    {
        _rpfManager = new RpfManager();
        _rpfManager.Init(gtaPath, false, Console.WriteLine, Console.Error.WriteLine);
        _logger = logger;
    }

    public List<string> SearchFile(string filename)
    {
        var results = new List<string>();
        foreach (var entry in _rpfManager.EntryDict.Values)
        {
            if (entry.Name.Contains(filename, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(entry.Path);
            }
        }
        return results;
    }

    public byte[] ExtractFile(string filename)
    {
        foreach (var entry in _rpfManager.EntryDict.Values)
        {
            if (entry.Name.Equals(filename, StringComparison.OrdinalIgnoreCase))
            {
                if (entry is RpfFileEntry fileEntry)
                {
                    _logger.LogDebug("Extracting {FilePath}...", fileEntry.Path);
                    return fileEntry.File.ExtractFile(fileEntry);
                }
            }
        }
        throw new FileNotFoundException($"File '{filename}' not found.");
    }

    public (byte[] fileBytes, RpfFileEntry entry)? ExtractFileWithEntry(string fullRpfPath)
    {
        foreach (var entry in _rpfManager.EntryDict.Values)
        {
            if (entry is RpfFileEntry fileEntry)
            {
                _logger.LogDebug("[DEBUG] {FilePath}", fileEntry.Path);
                if (fileEntry.Path.Equals(fullRpfPath, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("[MATCH] Found: {FilePath}", fileEntry.Path);
                    return (fileEntry.File.ExtractFile(fileEntry), fileEntry);
                }
            }
        }
        _logger.LogWarning("[DEBUG] File not found in RPF: {FullPath}", fullRpfPath);
        return null;
    }

    public RpfFile LoadRpf(string rpfPath)
    {
        _logger.LogDebug("[DEBUG] Attempting to open RPF file directly: {RpfPath}", rpfPath);

        if (!File.Exists(rpfPath))
        {
            _logger.LogError("[ERROR] RPF archive not found: {RpfPath}", rpfPath);
            throw new FileNotFoundException($"RPF archive not found: {rpfPath}");
        }

        RpfFile rpfFile = new RpfFile(rpfPath, Path.GetFileName(rpfPath));
        rpfFile.ScanStructure(msg => _logger.LogDebug(msg), err => _logger.LogError(err));

        if (rpfFile == null || rpfFile.AllEntries == null)
        {
            _logger.LogError("[ERROR] Failed to scan RPF structure: {RpfPath}", rpfPath);
            throw new Exception($"Failed to load RPF: {rpfPath}");
        }

        _logger.LogDebug("[DEBUG] Successfully loaded and scanned RPF: {RpfPath}", rpfPath);
        return rpfFile;
    }

    public RpfDirectoryEntry FindDirectoryInRpf(RpfFile rpfFile, string folderPath)
    {
        RpfDirectoryEntry? currentDir = rpfFile.Root;
        foreach (var part in folderPath.Split(Path.DirectorySeparatorChar))
        {
            currentDir = currentDir?.Directories.Find(d => d.Name.Equals(part, StringComparison.OrdinalIgnoreCase));
            if (currentDir == null)
            {
                throw new Exception($"Directory not found inside RPF: {folderPath}");
            }
        }
        return currentDir!;
    }

    public int Preheat()
    {
        int count = 0;
        foreach (var entry in _rpfManager.EntryDict.Values)
        {
            string path = entry.Path;
            count++;
        }
        return count;
    }
}