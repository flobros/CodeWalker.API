using System;
using System.Collections.Generic;
using System.IO;
using CodeWalker.GameFiles;
using CodeWalker.Utils;

public class TextureService
{
    public TextureService() { } // ✅ No need for GameFileCache

    public void ExportTexturesFromYdr(YdrFile ydr, string outputFolderPath, bool includeEmbedded = true)
    {
        Console.WriteLine($"[DEBUG] Attempting to export textures for: {ydr.Name}");
        if (ydr.Drawable == null)
        {
            Console.WriteLine($"[ERROR] Drawable is NULL for {ydr.Name}");
            return;
        }
        if (ydr.Drawable.ShaderGroup == null)
        {
            Console.WriteLine($"[ERROR] ShaderGroup is NULL for {ydr.Name}");
            return;
        }
        Console.WriteLine($"[DEBUG] YDR file loaded: {ydr.Name}");
        Directory.CreateDirectory(outputFolderPath);

        HashSet<Texture> textures = new HashSet<Texture>();
        var texturesMissing = new HashSet<string>();

        void CollectTextures(DrawableBase drawable)
        {
            if (drawable?.ShaderGroup?.Shaders?.data_items == null)
            {
                Console.WriteLine($"[ERROR] ShaderGroup or data_items is NULL in {ydr.Name}");
                return;
            }
            foreach (var shader in drawable.ShaderGroup.Shaders.data_items)
            {
                foreach (var param in shader.ParametersList.Parameters)
                {
                    if (param.Data is TextureBase texBase)
                    {
                        if (texBase is Texture tex)
                        {
                            if (includeEmbedded)
                            {
                                textures.Add(tex);
                            }
                        }
                        else
                        {
                            texturesMissing.Add(texBase.Name);
                        }
                    }
                }
            }
        }

        CollectTextures(ydr.Drawable);

        foreach (var tex in textures)
        {
            if (tex == null)
            {
                Console.WriteLine($"[ERROR] Encountered NULL texture in {ydr.Name}");
                continue;
            }
            string textureFilePath = Path.Combine(outputFolderPath, tex.Name + ".dds");
            byte[] ddsData = DDSIO.GetDDSFile(tex);
            if (ddsData == null)
            {
                Console.WriteLine($"[ERROR] DDS conversion failed for {tex.Name}");
                continue;
            }
            File.WriteAllBytes(textureFilePath, ddsData);
            Console.WriteLine($"[DEBUG] Saved texture: {textureFilePath}");
        }
        Console.WriteLine("[INFO] Texture export completed.");
    }

}
