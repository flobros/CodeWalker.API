namespace CodeWalker.API.Utils
{
    public static class PathUtils
    {
        public static string NormalizePath(string path, bool useBackslashes = false)
        {
            var parts = path
                .Replace('\\', '/')
                .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            var joined = string.Join("/", parts);
            return useBackslashes ? joined.Replace('/', '\\') : joined;
        }

        public static bool PathEquals(string a, string b)
        {
            return NormalizePath(a).Equals(NormalizePath(b), StringComparison.OrdinalIgnoreCase);
        }

    }


}
