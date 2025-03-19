using CodeWalker.GameFiles;
using CodeWalker.Utils;
using System;
using System.Collections.Generic;
using System.IO;

public class TextureExtractor
{
    private readonly GameFileCache _gameFileCache;
    private readonly ILogger<TextureExtractor> _logger; // ✅ Inject logger


    public TextureExtractor(GameFileCache cache, ILogger<TextureExtractor> logger) // ✅ Add logger
    {
        _gameFileCache = cache;
        _logger = logger;
    }

    public void ExtractTextures(byte[] fileBytes, RpfFileEntry entry, string outputFolder)
    {   
        bool hasEmbeddedTextures = false;   
        _logger.LogInformation($"Extracting textures from: {entry.Name}");

        // ✅ Load YDR from memory
        YdrFile ydr = LoadYdrFile(fileBytes, entry);
        if (ydr == null || ydr.Drawable == null)
        {
            _logger.LogError($"Failed to load YDR file: {entry.Name}");
            return;
        }

        HashSet<Texture> textures = new HashSet<Texture>();

        // ✅ Check for embedded textures
        if (ydr.Drawable.ShaderGroup?.TextureDictionary?.Textures?.data_items != null)
        {
            _logger.LogInformation($"Found {ydr.Drawable.ShaderGroup.TextureDictionary.Textures.data_items.Length} embedded textures.");
            hasEmbeddedTextures = true;
            foreach (var tex in ydr.Drawable.ShaderGroup.TextureDictionary.Textures.data_items)
            {
                textures.Add(tex);
            }
        }
        else
        {
            _logger.LogWarning($"No embedded textures found in {entry.Name}.");
        }

        if(!hasEmbeddedTextures)
            { 
            string ydrName = Path.GetFileNameWithoutExtension(entry.Name);
            uint modelHash = entry.ShortNameHash;

            // ✅ First, try direct hash from YDR name
            uint textureDictionaryHash = JenkHash.GenHash(ydrName);
            _logger.LogInformation($"Looking up YTD with direct Jenkins hash: {textureDictionaryHash}");

            // ✅ Second, try archetype-based texture dictionary
            var archetype = _gameFileCache.GetArchetype(modelHash);
            if (archetype != null)
            {
                _logger.LogInformation($"Using texture dictionary hash from archetype: {archetype.TextureDict.Hash}");
                textureDictionaryHash = archetype.TextureDict.Hash;
            }
            else
            {
                _logger.LogWarning($"No archetype found for model hash: {modelHash}");
            }

            // 🔍 Try to load YTD file (ONLY BY HASH)
            _logger.LogInformation($"🔍 Checking game cache for YTD with hash: {textureDictionaryHash}...");
            YtdFile ytd = _gameFileCache.GetYtd(textureDictionaryHash);

            RpfFileEntry ytdEntry = _gameFileCache.GetYtdEntry(textureDictionaryHash);
            byte[] ytdData = _gameFileCache.RpfMan.GetFileData(ytdEntry.Path);
            ytd.Load(ytdData, ytdEntry);
            ytd.Loaded = true;

            if (ytd == null)
            {
                _logger.LogWarning($"❌ YTD not found for hash: {textureDictionaryHash}");
                return;
            }
        
            if (ytd != null && ytd.TextureDict?.Textures?.data_items != null)
            {
                _logger.LogInformation($"✅ Found {ytd.TextureDict.Textures.data_items.Length} textures in YTD (hash {textureDictionaryHash}).");
                foreach (var tex in ytd.TextureDict.Textures.data_items)
                {
                    textures.Add(tex);
                }
            }
            else
            {
                _logger.LogWarning($"⚠️ YTD (hash {textureDictionaryHash}) loaded but contains NO textures.");
            }
        }

        _logger.LogInformation($"Total textures to save: {textures.Count}");
        SaveTextures(textures, outputFolder);
    }





    private YdrFile LoadYdrFile(byte[] fileBytes, RpfFileEntry entry)
    {
        // ✅ Check if the file is compressed and decompress it
        if (BitConverter.ToUInt32(fileBytes, 0) == 0x37435352) // "RSC7" header
        {
            _logger.LogInformation($"[INFO] Decompressing YDR file: {entry.Name}");
            fileBytes = ResourceBuilder.Decompress(fileBytes);
        }

        YdrFile ydr = new YdrFile();
        ydr.Load(fileBytes, entry); // ✅ Pass entry for correct loading

        if (ydr.Drawable?.ShaderGroup?.TextureDictionary == null)
        {
            _logger.LogWarning($"[WARNING] No embedded textures found in {entry.Name}");
        }
        else
        {
            _logger.LogInformation($"[INFO] Found {ydr.Drawable.ShaderGroup.TextureDictionary.Textures.data_items.Length} textures.");
        }

        return ydr;
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
                Console.WriteLine($"Failed to save texture {tex.Name}: {ex.Message}");
            }
        }
    }
}
