using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeWalker.API.Services;
using CodeWalker.API.Utils;
using CodeWalker.GameFiles;
using Microsoft.Extensions.Logging;

public class RpfService
{
    private readonly RpfManager _rpfManager;
    private readonly ILogger<RpfService> _logger;
    private readonly ConfigService _configService;

    public RpfService(ILogger<RpfService> logger, ConfigService configService)
    {
        _logger = logger;
        _configService = configService;
        string gtaPath = _configService.Get().GTAPath;

        _rpfManager = new RpfManager();
        _rpfManager.Init(gtaPath, false, Console.WriteLine, Console.Error.WriteLine);
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').Trim();
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
        var entry = _rpfManager.GetEntry(filename);
        if (entry is RpfFileEntry fileEntry)
        {
            _logger.LogDebug("Extracting {FilePath}...", fileEntry.Path);
            return fileEntry.File.ExtractFile(fileEntry);
        }
        throw new FileNotFoundException($"File '{filename}' not found.");
    }

    public (byte[] fileBytes, RpfFileEntry entry)? ExtractFileWithEntry(string fullRpfPath)
    {
        var entry = _rpfManager.GetEntry(fullRpfPath);
        if (entry is RpfFileEntry fileEntry)
        {
            _logger.LogDebug("[MATCH] Found: {FilePath}", fileEntry.Path);
            return (fileEntry.File.ExtractFile(fileEntry), fileEntry);
        }
        _logger.LogWarning("[DEBUG] File not found in RPF: {FullPath}", fullRpfPath);
        return null;
    }

    public RpfEntry? GetEntry(string path)
    {
        return _rpfManager.GetEntry(path);
    }

    public RpfFile? GetParentRpfFile(RpfEntry entry)
    {
        return entry?.File;
    }

    public RpfDirectoryEntry EnsureDirectoryPath(RpfDirectoryEntry root, string internalPath)
    {
        var parts = internalPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
        RpfDirectoryEntry current = root;
        foreach (var part in parts)
        {
            var existing = current.Directories.FirstOrDefault(d => d.Name.Equals(part, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                current = existing;
                continue;
            }
            _logger.LogInformation("[CREATE] Creating directory '{Dir}' in '{Parent}'", part, current.Path);
            var newDir = RpfFile.CreateDirectory(current, part);
            current = newDir;
        }
        return current;
    }

    private string ToVirtualPath(string fullPath)
    {
        var gtaPath = PathUtils.NormalizePath(_configService.Get().GTAPath); // `/`
        var normalized = PathUtils.NormalizePath(fullPath); // `/`

        if (normalized.StartsWith(gtaPath, StringComparison.OrdinalIgnoreCase))
            normalized = normalized.Substring(gtaPath.Length).TrimStart('/');

        return PathUtils.NormalizePath(normalized, useBackslashes: true); // final lookup version
    }


    public bool TryResolveEntryPath(string fullPath, out RpfFile? rpfFile, out RpfDirectoryEntry? targetDir, out string? fileName, out string? error)
    {
        rpfFile = null;
        targetDir = null;
        fileName = null;
        error = null;

        try
        {
            var normalized = NormalizePath(fullPath);
            _logger.LogDebug("[TryResolve] Normalized path: {Path}", normalized);

            var virtualPath = ToVirtualPath(fullPath); // now backslash-correct

            // Case 1: It’s a folder
            var entry = _rpfManager.GetEntry(virtualPath);
            if (entry is RpfDirectoryEntry dirEntry)
            {
                rpfFile = dirEntry.File;
                targetDir = dirEntry;
                return true;
            }

            // Case 2: It’s an RPF file
            if (virtualPath.EndsWith(".rpf", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("[TryResolve] Trying FindRpfFile({Path})", virtualPath);
                var archive = _rpfManager.FindRpfFile(virtualPath);
                if (archive != null)
                {
                    rpfFile = archive;
                    targetDir = rpfFile?.Root;
                    return true;
                }
            }

            error = $"Entry not found or not a valid target: {entry?.GetType().Name ?? "null"}";
            _logger.LogWarning("[TryResolve] {Error}", error);
            return false;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            _logger.LogError(ex, "[TryResolve] Unexpected error");
            return false;
        }
    }


















    public int Preheat()
    {
        return _rpfManager.EntryDict.Count;
    }

    public RpfManager Manager => _rpfManager;

}
