using System;
using System.Collections.Generic;

namespace NescaWpf.Models
{
    public class ScanHistoryItem
    {
        public DateTime ScanDate { get; set; }
        public string Summary { get; set; }
        public string FilePath { get; set; }
        public List<ScanResult> Results { get; set; } = new List<ScanResult>();
    }
}