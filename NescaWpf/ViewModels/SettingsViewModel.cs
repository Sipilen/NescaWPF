using NescaWpf.Models;

namespace NescaWpf.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private ScanSettings _settings = new ScanSettings();
        public ScanSettings Settings
        {
            get => _settings;
            set
            {
                _settings = value;
                OnPropertyChanged();
            }
        }
    }
}