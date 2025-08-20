using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NescaWpf.Models;

namespace NescaWpf.Helpers
{
    public static class CategoryHtmlAppender
    {
        private static readonly object _lock = new object();
        private static readonly string ExportDir = "exports";
        private static readonly Dictionary<string, bool> FileInitialized = new Dictionary<string, bool>();
        private static readonly Dictionary<string, List<string>> _buffer = new Dictionary<string, List<string>>();
        private static readonly int BufferSize = 10;

        static CategoryHtmlAppender()
        {
            if (!Directory.Exists(ExportDir))
            {
                Directory.CreateDirectory(ExportDir);
            }
        }

        public static void Append(ScanResult result)
        {
            string category = GetCategory(result);
            string filePath = Path.Combine(ExportDir, $"{category}.html");

            lock (_lock)
            {
                if (!FileInitialized.ContainsKey(filePath) || !FileInitialized[filePath])
                {
                    File.WriteAllText(filePath, $"<DOCUMENT filename=\"{category}.html\">\n");
                    FileInitialized[filePath] = true;
                    _buffer[filePath] = new List<string>();
                }

                var sb = new StringBuilder();
                string title = string.IsNullOrEmpty(result.ServiceType) ? "Unknown" : result.ServiceType;
                if (!string.IsNullOrEmpty(result.Vulnerability))
                {
                    title += $" ({result.Vulnerability})";
                }
                string snippet = result.Banner.Length > 100 ? result.Banner.Substring(0, 100) : result.Banner;
                string date = $"[{result.Time:ddd MMM dd HH:mm:ss yyyy}]";

                sb.AppendLine($"{result.Ip}:{result.Port} : {title}");
                sb.AppendLine("<div class=\"login_wrapper\">");
                sb.AppendLine("  <div cla.camera \t.other \t.auth \t.ftp \t.ssh");
                sb.AppendLine();
                sb.AppendLine($"{date} {result.Ip}:{result.Port}; Received: {result.Banner.Length}; T: {title}");
                sb.AppendLine("<div class=\"login_wrapper\">");
                sb.AppendLine($"  <div class=\"animate form lo");
                sb.AppendLine();

                _buffer[filePath].Add(sb.ToString());

                if (_buffer[filePath].Count >= BufferSize)
                {
                    File.AppendAllText(filePath, string.Join("", _buffer[filePath]));
                    _buffer[filePath].Clear();
                }
            }
        }

        public static void FlushAll()
        {
            lock (_lock)
            {
                foreach (var kvp in _buffer)
                {
                    if (kvp.Value.Count > 0)
                    {
                        File.AppendAllText(kvp.Key, string.Join("", kvp.Value));
                        kvp.Value.Clear();
                    }
                }
            }
        }

        private static string GetCategory(ScanResult result)
        {
            string bannerLower = result.Banner.ToLowerInvariant();
            string vulnLower = result.Vulnerability.ToLowerInvariant();
            string serviceLower = result.ServiceType.ToLowerInvariant();

            if (serviceLower.Contains("ftp") || vulnLower.Contains("ftp"))
            {
                return "ftp";
            }
            if (serviceLower.Contains("http") && (bannerLower.Contains("login") || bannerLower.Contains("auth") || bannerLower.Contains("password") || vulnLower.Contains("пароль")))
            {
                return "auth";
            }
            return "other";
        }
    }
}