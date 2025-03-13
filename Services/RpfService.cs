using System;
using System.Collections.Generic;
using System.IO;
using CodeWalker.GameFiles;

public class RpfService
{
    private readonly RpfManager _rpfManager;

    public RpfService(string gtaPath)
    {
        _rpfManager = new RpfManager();
        _rpfManager.Init(gtaPath, false, Console.WriteLine, Console.Error.WriteLine);
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

    public (byte[] fileBytes, RpfFileEntry entry)? ExtractFileWithEntry(string filename)
    {
        foreach (var entry in _rpfManager.EntryDict.Values)
        {
            if (entry.Name.Equals(filename, StringComparison.OrdinalIgnoreCase))
            {
                if (entry is RpfFileEntry fileEntry)
                {
                    return (fileEntry.File.ExtractFile(fileEntry), fileEntry);
                }
            }
        }
        return null; // ✅ Ensure the method can return null
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
        // Navigate through the RPF structure
        RpfDirectoryEntry currentDir = rpfFile.Root;
        foreach (var part in folderPath.Split(Path.DirectorySeparatorChar))
        {
            currentDir = currentDir.Directories.Find(d => d.Name.Equals(part, StringComparison.OrdinalIgnoreCase));
            if (currentDir == null)
            {
                throw new Exception($"Directory not found inside RPF: {folderPath}");
            }
        }
        return currentDir;
    }



}
