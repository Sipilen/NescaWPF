using NescaWpf.Models;
using NescaWpf.Services;
using System.Collections.Generic;
using System.Windows.Controls;

namespace NescaWpf.ViewModels
{
    public class StatisticsViewModel : ViewModelBase
    {
        private readonly StatisticsService _statisticsService = new StatisticsService();
        private string _summary = "Нет данных о статистике.";
        public string Summary
        {
            get => _summary;
            set
            {
                _summary = value;
                OnPropertyChanged();
            }
        }

        public void UpdateChart(WebBrowser webBrowser, IEnumerable<ScanResult> results)
        {
            _statisticsService.UpdateChart(webBrowser, results);
        }

        internal void UpdateChart(WebBrowser statsWebBrowser)
        {
        }

        internal void UpdateChart(object statsWebBrowser)
        {
            throw new NotImplementedException();
        }
    }
}