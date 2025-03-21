using System;
using System.Collections.Generic;
using System.IO;
using CodeWalker.GameFiles;


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
                    Console.WriteLine($"Extracting {fileEntry.Path}...");
                    return fileEntry.File.ExtractFile(fileEntry);
                }
            }
        }
        throw new FileNotFoundException($"File '{filename}' not found.");
    }

    public (byte[] fileBytes, RpfFileEntry entry)? ExtractFileWithEntry(string fullRpfPath)
    {
        Console.WriteLine($"[DEBUG] Searching for file in RPF: {fullRpfPath}");

        //_logger.LogDebug("Listing all RPF entries:");
        //Console.WriteLine("[DEBUG] Listing all RPF entries:");

        foreach (var entry in _rpfManager.EntryDict.Values)
        {
            if (entry is RpfFileEntry fileEntry)
            {
                string entryFullPath = fileEntry.Path;

                //// Print EXACT match comparison
                //Console.WriteLine($"[DEBUG] Comparing requested: '{fullRpfPath}'");
                //Console.WriteLine($"[DEBUG]       against entry: '{entryFullPath}'");

                // Check if they match
                if (entryFullPath.Equals(fullRpfPath, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"[MATCH] Found: {entryFullPath}");
                    _logger.LogInformation($"Match found for: {entryFullPath}");
                    return (fileEntry.File.ExtractFile(fileEntry), fileEntry);
                }
            }
        }

        Console.WriteLine($"[DEBUG] File not found in RPF: {fullRpfPath}");
        _logger.LogWarning($"File not found in RPF: {fullRpfPath}");
        return null;
    }





    public RpfFile LoadRpf(string rpfPath)
    {
        Console.WriteLine($"[DEBUG] Attempting to open RPF file directly: {rpfPath}");

        if (!File.Exists(rpfPath))
        {
            Console.WriteLine($"[ERROR] RPF archive not found: {rpfPath}");
            throw new FileNotFoundException($"RPF archive not found: {rpfPath}");
        }

        // **Manually open the RPF file like CodeWalker does**
        RpfFile rpfFile = new RpfFile(rpfPath, Path.GetFileName(rpfPath));
        rpfFile.ScanStructure(Console.WriteLine, Console.Error.WriteLine);

        if (rpfFile == null || rpfFile.AllEntries == null)
        {
            Console.WriteLine($"[ERROR] Failed to scan RPF structure: {rpfPath}");
            throw new Exception($"Failed to load RPF: {rpfPath}");
        }

        Console.WriteLine($"[DEBUG] Successfully loaded and scanned RPF: {rpfPath}");
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



}
