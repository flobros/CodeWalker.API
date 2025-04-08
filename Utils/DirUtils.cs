namespace CodeWalker.API.Utils
{
    public static class DirUtils
    {
        public static void CopyDirectory(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);
            foreach (var file in Directory.GetFiles(sourceDir))
                System.IO.File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), true);

            foreach (var dir in Directory.GetDirectories(sourceDir))
                CopyDirectory(dir, Path.Combine(targetDir, Path.GetFileName(dir)));
        }

    }


}
