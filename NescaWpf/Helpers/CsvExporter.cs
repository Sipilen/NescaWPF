using System.Collections.Generic;
using System.IO;
using NescaWpf.Models;

namespace NescaWpf.Helpers
{
    public static class CsvExporter
    {
        public static void Export(IEnumerable<ScanResult> results, string filePath)
        {
            var lines = new List<string> { "IP,Port,Protocol,ServiceType,Vulnerability,SpecialFlag,Time" };
            lines.AddRange(results.Select(r => $"{r.Ip},{r.Port},{r.Protocol},{r.ServiceType},{r.Vulnerability},{r.SpecialFlag},{r.Time}"));
            File.WriteAllLines(filePath, lines);
        }
    }
}