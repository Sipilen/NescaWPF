using NescaWpf.Models;

namespace NescaWpf.ViewModels
{
    public class ScanResultViewModel : ViewModelBase
    {
        private ScanResult _result;
        public ScanResult Result
        {
            get => _result;
            set
            {
                _result = value;
                OnPropertyChanged();
            }
        }
    }
}