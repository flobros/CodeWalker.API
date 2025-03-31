using CodeWalker.GameFiles;
using CodeWalker.Utils;
using System;
using System.Collections.Generic;
using System.IO;

public class TextureExtractor
{
    private readonly GameFileCache _gameFileCache;
    private readonly ILogger<TextureExtractor> _logger;

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

        var tryGetTextureFromYtd = new Func<uint, YtdFile, Texture?>((texHash, ytd) =>
        {
            if (ytd == null) return null;
            int tries = 0;
            while (!ytd.Loaded && tries < 500)
            {
                System.Threading.Thread.Sleep(10);
                tries++;
            }
            return ytd.Loaded ? ytd.TextureDict?.Lookup(texHash) : null;
        });

        var tryGetTexture = new Func<uint, uint, Texture?>((texHash, txdHash) =>
        {
            if (txdHash == 0) return null;

            var ytdEntry = _gameFileCache.YtdDict.TryGetValue(txdHash, out var entry) ? entry : null;
            var ytd = _gameFileCache.GetYtd(txdHash);

            if (ytd == null)
                return null;

            if (!ytd.Loaded && ytdEntry != null)
            {
                var data = _gameFileCache.RpfMan.GetFileData(ytdEntry.Path);
                ytd.Load(data, ytdEntry);
                ytd.Loaded = true;
                _logger.LogInformation($"✅ Manually loaded YTD: {ytdEntry.Path}");
            }

            // By this point, `ytd` is confirmed non-null
            return tryGetTextureFromYtd(texHash, ytd);
        });


        var collectTextures = new Action<DrawableBase>((drawable) =>
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
                        var texResult = tryGetTexture(texhash, txdHash);

                        if (texResult == null)
                        {
                            var ptxdhash = _gameFileCache.TryGetParentYtdHash(txdHash);
                            while (ptxdhash != 0 && texResult == null)
                            {
                                texResult = tryGetTexture(texhash, ptxdhash);
                                if (texResult == null)
                                {
                                    ptxdhash = _gameFileCache.TryGetParentYtdHash(ptxdhash);
                                }
                            }
                        }

                        if (texResult == null)
                        {
                            var ytd = _gameFileCache.TryGetTextureDictForTexture(texhash);
                            texResult = tryGetTextureFromYtd(texhash, ytd);
                        }

                        if (texResult == null)
                        {
                            if (!string.IsNullOrEmpty(baseTex.Name))
                                missing.Add(baseTex.Name);
                            _logger.LogWarning($"Missing texture: {baseTex.Name}");
                        }
                        else
                        {
                            textures.Add(texResult);
                        }
                    }
                }
            }
        });

        switch (ext)
        {
            case ".ydr":
                var ydr = RpfFile.GetFile<YdrFile>(entry, fileBytes);
                if (ydr?.Drawable != null)
                    collectTextures(ydr.Drawable);
                break;
            case ".yft":
                var yft = RpfFile.GetFile<YftFile>(entry, fileBytes);
                var f = yft?.Fragment;
                if (f != null)
                {
                    collectTextures(f.Drawable);
                    collectTextures(f.DrawableCloth);
                    if (f.DrawableArray?.data_items != null)
                    {
                        foreach (var d in f.DrawableArray.data_items)
                            collectTextures(d);
                    }
                    if (f.Cloths?.data_items != null)
                    {
                        foreach (var c in f.Cloths.data_items)
                            collectTextures(c.Drawable);
                    }
                    var children = f.PhysicsLODGroup?.PhysicsLOD1?.Children?.data_items;
                    if (children != null)
                    {
                        foreach (var child in children)
                        {
                            collectTextures(child.Drawable1);
                            collectTextures(child.Drawable2);
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