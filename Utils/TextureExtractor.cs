using CodeWalker.GameFiles;
using CodeWalker.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class TextureExtractor
{
    private readonly GameFileCache _gameFileCache;
    private readonly ILogger<TextureExtractor> _logger;

    /// <summary>
    /// Caches successful brute-force texture lookups (texHash → YTD path) to avoid
    /// repeating the expensive full-scan fallback for textures seen before.
    /// </summary>
    private static readonly Dictionary<uint, string> _bruteForceCache = new();
    private static readonly object _bruteForceCacheLock = new();

    /// <summary>
    /// Maximum number of YTD entries to probe during the brute-force fallback.
    /// Prevents unbounded I/O when a texture genuinely does not exist.
    /// </summary>
    private const int MaxBruteForceIterations = 100;

    public TextureExtractor(GameFileCache cache, ILogger<TextureExtractor> logger)
    {
        _gameFileCache = cache;
        _logger = logger;
    }

    public void ExtractTextures(byte[] fileBytes, RpfFileEntry entry, string outputFolder)
    {
        string ext = Path.GetExtension(entry.Name)?.ToLowerInvariant() ?? string.Empty;
        _logger.LogInformation($"Extracting textures from: {entry.Name} (ext: {ext})");

        HashSet<Texture> textures = new();
        HashSet<string> missing = new();

        Texture? TryGetTextureFromYtd(uint texHash, YtdFile? ytd)
        {
            if (ytd == null) return null;
            if (!ytd.Loaded)
            {
                _logger.LogWarning("YTD was not marked as loaded before access.");
                return null;
            }
            var tex = ytd.TextureDict?.Lookup(texHash);
            _logger.LogDebug($"▶ texHash {texHash:X8} → Found in TextureDict: {(tex != null)}");
            return tex;
        }

        Texture? TryGetTexture(uint texHash, uint txdHash)
        {
            Texture? texResult = null;

            if (txdHash != 0)
            {
                if (_gameFileCache.YtdDict.TryGetValue(txdHash, out var ytdEntry) && ytdEntry != null)
                {
                    var ytd = _gameFileCache.GetYtd(txdHash);
                    if (ytd == null || !ytd.Loaded)
                    {
                        try
                        {
                            var data = _gameFileCache.RpfMan.GetFileData(ytdEntry.Path);
                            ytd = new YtdFile();
                            ytd.Load(data, ytdEntry);
                            ytd.Loaded = true;
                            _logger.LogDebug($"✅ Lazily loaded YTD: {ytdEntry.Path}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"❌ Failed to load YTD {ytdEntry.Path}: {ex.Message}");
                            return null;
                        }
                    }
                    texResult = TryGetTextureFromYtd(texHash, ytd);
                    if (texResult != null)
                        return texResult;
                }
            }

            var visited = new HashSet<uint>();
            var ptxdhash = _gameFileCache.TryGetParentYtdHash(txdHash);
            while (ptxdhash != 0 && texResult == null && !visited.Contains(ptxdhash))
            {
                visited.Add(ptxdhash);
                texResult = TryGetTexture(texHash, ptxdhash);
                if (texResult == null)
                {
                    ptxdhash = _gameFileCache.TryGetParentYtdHash(ptxdhash);
                }
            }

            if (texResult == null)
            {
                var fallbackYtd = _gameFileCache.TryGetTextureDictForTexture(texHash);
                texResult = TryGetTextureFromYtd(texHash, fallbackYtd);
            }

            if (texResult == null)
            {
                // Check the brute-force cache first to avoid redundant full scans
                string? cachedPath = null;
                lock (_bruteForceCacheLock)
                {
                    _bruteForceCache.TryGetValue(texHash, out cachedPath);
                }

                if (cachedPath != null && _gameFileCache.YtdDict.Values.FirstOrDefault(e => e?.Path == cachedPath) is { } cachedEntry)
                {
                    try
                    {
                        var data = _gameFileCache.RpfMan.GetFileData(cachedEntry.Path);
                        var cachedYtd = new YtdFile();
                        cachedYtd.Load(data, cachedEntry);
                        cachedYtd.Loaded = true;
                        texResult = TryGetTextureFromYtd(texHash, cachedYtd);
                        if (texResult != null)
                        {
                            _logger.LogDebug($"💥 Brute-force cache hit for {texHash:X8} in {cachedEntry.Path}");
                            return texResult;
                        }
                    }
                    catch { /* cache entry stale – fall through to limited scan */ }
                }

                // Limited brute-force scan (capped at MaxBruteForceIterations)
                _logger.LogWarning($"⚠️ Starting limited brute-force texture scan for {texHash:X8} (max {MaxBruteForceIterations} entries)");
                int iterations = 0;
                foreach (var fallbackEntry in _gameFileCache.YtdDict.Values)
                {
                    if (fallbackEntry == null) continue;
                    if (++iterations > MaxBruteForceIterations)
                    {
                        _logger.LogWarning($"⚠️ Brute-force limit ({MaxBruteForceIterations}) reached for texture {texHash:X8}. Giving up.");
                        break;
                    }
                    try
                    {
                        var data = _gameFileCache.RpfMan.GetFileData(fallbackEntry.Path);
                        var fallbackYtd = new YtdFile();
                        fallbackYtd.Load(data, fallbackEntry);
                        fallbackYtd.Loaded = true;
                        texResult = TryGetTextureFromYtd(texHash, fallbackYtd);
                        if (texResult != null)
                        {
                            _logger.LogDebug($"💥 Fallback hit in {fallbackEntry.Path} (after {iterations} probes)");
                            lock (_bruteForceCacheLock)
                            {
                                _bruteForceCache[texHash] = fallbackEntry.Path;
                            }
                            return texResult;
                        }
                    }
                    catch { }
                }
            }

            return texResult;
        }

        void CollectTextures(DrawableBase drawable)
        {
            if (drawable == null) return;
            var embedded = drawable.ShaderGroup?.TextureDictionary?.Textures?.data_items;
            if (embedded != null)
            {
                foreach (var tex in embedded)
                    textures.Add(tex);
            }

            if (drawable.Owner is YptFile ypt && ypt.PtfxList?.TextureDictionary?.Textures?.data_items != null)
            {
                foreach (var tex in ypt.PtfxList.TextureDictionary.Textures.data_items)
                    textures.Add(tex);
                return;
            }

            var shaders = drawable.ShaderGroup?.Shaders?.data_items;
            if (shaders == null) return;

            uint archHash = 0;
            if (drawable is Drawable dwbl)
            {
                var name = dwbl.Name?.ToLowerInvariant()?.Replace(".#dr", "")?.Replace(".#dd", "");
                if (!string.IsNullOrEmpty(name)) archHash = JenkHash.GenHash(name);
            }
            else if (drawable is FragDrawable fdbl && fdbl.Owner is YftFile yftFile && yftFile.RpfFileEntry != null)
            {
                archHash = (uint)yftFile.RpfFileEntry.ShortNameHash;
            }

            var arch = _gameFileCache.GetArchetype(archHash);
            if (arch == null)
            {
                _logger.LogWarning($"⚠️ No archetype found for hash {archHash:X8} (name: '{(drawable is Drawable d ? d.Name : "unknown")}')");
            }

            var txdHash = (arch != null) ? arch.TextureDict.Hash : archHash;

            foreach (var s in shaders)
            {
                if (s?.ParametersList?.Parameters == null) continue;
                foreach (var p in s.ParametersList.Parameters)
                {
                    if (p?.Data is Texture tex)
                    {
                        textures.Add(tex);
                    }
                    else if (p?.Data is TextureBase baseTex)
                    {
                        var texhash = baseTex.NameHash;
                        var texResult = TryGetTexture(texhash, txdHash);

                        if (texResult == null && !string.IsNullOrEmpty(baseTex.Name))
                        {
                            missing.Add(baseTex.Name);
                            _logger.LogWarning($"Missing texture: {baseTex.Name}");
                        }
                        else if (texResult != null)
                        {
                            textures.Add(texResult);
                        }
                    }
                }
            }
        }

        switch (ext)
        {
            case ".ydr":
                var ydr = RpfFile.GetFile<YdrFile>(entry, fileBytes);
                if (ydr?.Drawable != null)
                    CollectTextures(ydr.Drawable);
                break;

            case ".yft":
                _logger.LogDebug("▶ Begin YFT processing");
                var yft = RpfFile.GetFile<YftFile>(entry, fileBytes);
                var f = yft?.Fragment;
                if (f != null)
                {
                    CollectTextures(f.Drawable);
                    _logger.LogDebug("▶ After Drawable");
                    CollectTextures(f.DrawableCloth);
                    _logger.LogDebug("▶ After DrawableCloth");

                    if (f.DrawableArray?.data_items != null)
                    {
                        foreach (var d in f.DrawableArray.data_items)
                            CollectTextures(d);
                    }

                    if (f.Cloths?.data_items != null)
                    {
                        foreach (var c in f.Cloths.data_items)
                            CollectTextures(c.Drawable);
                    }

                    var children = f.PhysicsLODGroup?.PhysicsLOD1?.Children?.data_items;
                    if (children != null)
                    {
                        foreach (var child in children)
                        {
                            CollectTextures(child.Drawable1);
                            CollectTextures(child.Drawable2);
                        }
                    }
                }
                break;
        }

        _logger.LogInformation($"Total textures to save: {textures.Count}");
        SaveTextures(textures, outputFolder);
    }

    private void SaveTextures(HashSet<Texture> textures, string folder)
    {
        foreach (var tex in textures)
        {
            try
            {
                byte[] dds = DDSIO.GetDDSFile(tex);
                string fpath = Path.Combine(folder, tex.Name + ".dds");
                File.WriteAllBytes(fpath, dds);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to save texture {tex.Name}: {ex.Message}");
            }
        }
    }
}