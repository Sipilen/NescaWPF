using System.Collections.Generic;
using System.IO;
using NescaWpf.Models;

namespace NescaWpf.Helpers
{
    public static class HtmlExporter
    {
        public static void Export(IEnumerable<ScanResult> results, string filePath)
        {
            var html = "<html><body><table border='1'>";
            html += "<tr><th>IP</th><th>Port</th><th>Protocol</th><th>Service</th><th>Vulnerability</th><th>SpecialFlag</th><th>Time</th></tr>";
            foreach (var r in results)
            {
                html += $"<tr><td>{r.Ip}</td><td>{r.Port}</td><td>{r.Protocol}</td><td>{r.ServiceType}</td><td>{r.Vulnerability}</td><td>{r.SpecialFlag}</td><td>{r.Time}</td></tr>";
            }
            html += "</table></body></html>";
            File.WriteAllText(filePath, html);
        }
    }
}