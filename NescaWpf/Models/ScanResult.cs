using System;

namespace NescaWpf.Models
{
    public class ScanResult
    {
        public string Ip { get; set; }
        public int Port { get; set; }
        public string Protocol { get; set; }
        public string Banner { get; set; }
        public string ServiceType { get; set; }
        public string Vulnerability { get; set; }
        public string SpecialFlag { get; set; }
        public DateTime Time { get; set; }
        public bool IsNew { get; set; }
    }
}