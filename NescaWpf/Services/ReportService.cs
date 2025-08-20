using System.Collections.Generic;
using NescaWpf.Models;
using NescaWpf.Helpers;

namespace NescaWpf.Services
{
    public class ReportService
    {
        public void ExportToCsv(IEnumerable<ScanResult> results, string filePath)
        {
            CsvExporter.Export(results, filePath);
        }

        public void ExportToTxt(IEnumerable<ScanResult> results, string filePath)
        {
            TxtExporter.Export(results, filePath);
        }

        public void ExportToHtml(IEnumerable<ScanResult> results, string filePath)
        {
            HtmlExporter.Export(results, filePath);
        }

        public void AutoAppendToCategoryHtml(ScanResult result)
        {
            CategoryHtmlAppender.Append(result);
        }
    }
}