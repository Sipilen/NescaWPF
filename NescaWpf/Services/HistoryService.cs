using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using NescaWpf.Models;

namespace NescaWpf.Services
{
    public class HistoryService
    {
        private readonly string _historyFile = "scan_history.json";

        public void SaveHistory(ScanHistoryItem historyItem)
        {
            var history = LoadHistory();
            history.Add(historyItem);
            var json = JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_historyFile, json);
        }

        public List<ScanHistoryItem> LoadHistory()
        {
            if (File.Exists(_historyFile))
            {
                var json = File.ReadAllText(_historyFile);
                return JsonSerializer.Deserialize<List<ScanHistoryItem>>(json) ?? new List<ScanHistoryItem>();
            }
            return new List<ScanHistoryItem>();
        }

        public (List<ScanResult> New, List<ScanResult> Removed) CompareScans(List<ScanResult> oldResults, List<ScanResult> newResults)
        {
            var oldSet = new HashSet<string>(oldResults.Select(r => $"{r.Ip}:{r.Port}"));
            var newSet = new HashSet<string>(newResults.Select(r => $"{r.Ip}:{r.Port}"));
            var added = newResults.Where(r => !oldSet.Contains($"{r.Ip}:{r.Port}")).ToList();
            var removed = oldResults.Where(r => !newSet.Contains($"{r.Ip}:{r.Port}")).ToList();
            return (added, removed);
        }
    }
}