namespace CodeWalker.API.Models
{
    public class ApiConfig
    {
        public string CodewalkerOutputDir { get; set; } = "";
        public string BlenderOutputDir { get; set; } = "";
        public string FivemOutputDir { get; set; } = "";
        public string RpfArchivePath { get; set; } = "";
        public string GTAPath { get; set; } = "";
        public bool Gen9 { get; set; } = false;
        public string Dlc { get; set; } = "";
        public bool EnableMods { get; set; } = false;
        public int Port { get; set; } = 0;
    }
}

