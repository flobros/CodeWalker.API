namespace CodeWalker.API.Models
{
    public class ApiConfig
    {
        public string CodewalkerOutputDir { get; set; } = @"C:\GTA_FILES\cw_out";
        public string BlenderOutputDir { get; set; } = @"C:\GTA_FILES\blender_out";
        public string FivemOutputDir { get; set; } = @"C:\GTA_FILES\fivem_out";
        public string RpfArchivePath { get; set; } = @"C:\Program Files\Rockstar Games\Grand Theft Auto V\modstore\new.rpf";
        public string GTAPath { get; set; } = @"C:\Program Files\Rockstar Games\Grand Theft Auto V";

        public int Port { get; set; } = 5555;
    }
}
