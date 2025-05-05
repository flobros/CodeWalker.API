namespace CodeWalker.API.Models
{
    public class ApiConfig
    {
        public string CodewalkerOutputDir { get; set; } = "";
        public string BlenderOutputDir { get; set; } = "";
        public string FivemOutputDir { get; set; } = "";
        public string RpfArchivePath { get; set; } = "";
        public string GTAPath { get; set; } = "";
        public int Port { get; set; } = 0;
    }
}

