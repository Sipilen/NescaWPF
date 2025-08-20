using System.Collections.Generic;
using System.IO;
using NescaWpf.Models;

namespace NescaWpf.Helpers
{
    public static class TxtExporter
    {
        public static void Export(IEnumerable<ScanResult> results, string filePath)
        {
            var lines = results.Select(r => $"IP: {r.Ip}, Port: {r.Port}, Service: {r.ServiceType}, Vulnerability: {r.Vulnerability}");
            File.WriteAllLines(filePath, lines);
        }
    }
}