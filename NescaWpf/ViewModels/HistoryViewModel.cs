using System.Collections.ObjectModel;
using NescaWpf.Models;

namespace NescaWpf.ViewModels
{
    public class HistoryViewModel : ViewModelBase
    {
        private ObservableCollection<ScanHistoryItem> _history = new ObservableCollection<ScanHistoryItem>();
        public ObservableCollection<ScanHistoryItem> History
        {
            get => _history;
            set
            {
                _history = value;
                OnPropertyChanged();
            }
        }
    }
}