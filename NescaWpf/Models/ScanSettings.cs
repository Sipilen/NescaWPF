namespace NescaWpf.Models
{
    public class ScanSettings
    {
        public string IpRanges { get; set; }
        public string IpExcludes { get; set; }
        public string Ports { get; set; }
        public int Threads { get; set; } = 500;
        public int Timeout { get; set; } = 800;
        public bool EnableTcp { get; set; } = true;
        public bool EnableUdp { get; set; } = false;
    }
}