using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using NescaWpf.Models;
using NescaWpf.Services;
using NescaWpf.Helpers;

namespace NescaWpf.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly ScannerService _scannerService = new ScannerService();
        private readonly ReportService _reportService = new ReportService();
        private readonly BannerParserService _bannerParser = new BannerParserService();
        private readonly VulnerabilityCheckerService _vulnChecker = new VulnerabilityCheckerService();
        private readonly HistoryService _historyService = new HistoryService();
        private readonly PresetService _presetService = new PresetService();
        private readonly NetworkGuardService _networkGuard = new NetworkGuardService();
        private TabControl _tabControl;
        private DispatcherTimer _uiUpdateTimer;
        private DispatcherTimer _chartUpdateTimer;

        private ScanSettings _scanSettings = new ScanSettings { EnableTcp = true, EnableUdp = true, Threads = 1000, Timeout = 1000 };
        public ScanSettings ScanSettings
        {
            get => _scanSettings;
            set
            {
                _scanSettings = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> PortPresets { get; set; } = new ObservableCollection<string>
        {
            "Web (80,443,8080,8000,8443)", "FTP (21,2121)", "Camera (554,37777,34567)", "IoT/SCADA", "Все (1-1024)"
        };
        private string _selectedPreset;
        public string SelectedPreset
        {
            get => _selectedPreset;
            set
            {
                _selectedPreset = value;
                if (!string.IsNullOrEmpty(value))
                {
                    ScanSettings.Ports = _presetService.GetPortPresets()[value];
                }
                OnPropertyChanged();
            }
        }
        private ObservableCollection<ScanResultViewModel> _filteredResults = new ObservableCollection<ScanResultViewModel>();
        public ObservableCollection<ScanResultViewModel> FilteredResults
        {
            get => _filteredResults;
            set
            {
                _filteredResults = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<string> ServiceTypes { get; set; } = new ObservableCollection<string>
        {
            "HTTP", "HTTPS", "FTP", "SSH", "Telnet", "RTSP", "CAM", "SCADA", "Other"
        };
        private string _selectedServiceType;
        public string SelectedServiceType
        {
            get => _selectedServiceType;
            set
            {
                _selectedServiceType = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }
        private string _filterText;
        public string FilterText
        {
            get => _filterText;
            set
            {
                _filterText = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }
        public ObservableCollection<string> LogLines { get; set; } = new ObservableCollection<string>();
        private string _status = "Готов к сканированию";
        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }
        private int _progress;
        public int Progress
        {
            get => _progress;
            set
            {
                _progress = value;
                OnPropertyChanged();
            }
        }
        private int _totalTasks;
        public int TotalTasks
        {
            get => _totalTasks;
            set
            {
                _totalTasks = value;
                OnPropertyChanged();
            }
        }
        private bool _canStart = true;
        public bool CanStart
        {
            get => _canStart;
            set
            {
                _canStart = value;
                OnPropertyChanged();
            }
        }
        private bool _canPause;
        public bool CanPause
        {
            get => _canPause;
            set
            {
                _canPause = value;
                OnPropertyChanged();
            }
        }
        private bool _canResume;
        public bool CanResume
        {
            get => _canResume;
            set
            {
                _canResume = value;
                OnPropertyChanged();
            }
        }
        private bool _canStop;
        public bool CanStop
        {
            get => _canStop;
            set
            {
                _canStop = value;
                OnPropertyChanged();
            }
        }
        private bool _showOnlyVulnerable;
        public bool ShowOnlyVulnerable
        {
            get => _showOnlyVulnerable;
            set
            {
                _showOnlyVulnerable = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }
        private bool _showOnlyNew;
        public bool ShowOnlyNew
        {
            get => _showOnlyNew;
            set
            {
                _showOnlyNew = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }
        private ObservableCollection<ScanHistoryItem> _scanHistory;
        public ObservableCollection<ScanHistoryItem> ScanHistory
        {
            get => _scanHistory;
            set
            {
                _scanHistory = value;
                OnPropertyChanged();
            }
        }
        public StatisticsViewModel Statistics { get; set; } = new StatisticsViewModel();
        private string _faqText;
        public string FaqText
        {
            get => _faqText;
            set
            {
                _faqText = value;
                OnPropertyChanged();
            }
        }

        public ICommand StartScanCommand { get; }
        public ICommand PauseScanCommand { get; }
        public ICommand ResumeScanCommand { get; }
        public ICommand StopScanCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand SaveSettingsCommand { get; }
        public ICommand ShowHistoryCommand { get; }
        public ICommand ShowFaqCommand { get; }
        public ICommand OpenInBrowserCommand { get; }
        public ICommand RunExternalScriptCommand { get; }
        public ICommand ClearResultsCommand { get; }
        public ICommand LoadHistoryCommand { get; }

        private List<ScanResultViewModel> _allScanResults = new List<ScanResultViewModel>();
        private List<ScanResultViewModel> _resultBuffer = new List<ScanResultViewModel>();

        public MainViewModel()
        {
            _scanHistory = new ObservableCollection<ScanHistoryItem>(_historyService.LoadHistory());
            try
            {
                _faqText = System.IO.File.ReadAllText("FAQ/faq.md");
            }
            catch
            {
                _faqText = "Часто задаваемые вопросы:\n1. Как начать сканирование?\nНажмите кнопку 'Старт' после настройки параметров.";
            }

            // UI батчинг
            _uiUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _uiUpdateTimer.Tick += (s, e) => FlushUiBuffer();

            // График раз в 5 сек
            _chartUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _chartUpdateTimer.Tick += (s, e) =>
            {
                Statistics.UpdateChart(null, FilteredResults.Select(r => r.Result));
                Statistics.Summary = $"Найдено результатов: {FilteredResults.Count}";
            };

            _scannerService.OnResult += result =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    result.ServiceType = _bannerParser.ParseBanner(result.Banner);
                    result.Vulnerability = _vulnChecker.CheckVulnerability(result);
                    var viewModel = new ScanResultViewModel { Result = result };
                    _allScanResults.Add(viewModel);
                    _resultBuffer.Add(viewModel);

                    _reportService.AutoAppendToCategoryHtml(result);

                    if (!_uiUpdateTimer.IsEnabled)
                    {
                        _uiUpdateTimer.Start();
                    }
                    if (!_chartUpdateTimer.IsEnabled)
                    {
                        _chartUpdateTimer.Start();
                    }
                });
            };
            _scannerService.OnProgress += (processed, total) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Progress = processed;
                    TotalTasks = total;
                    Status = $"Обработано {processed} из {total}";
                });
            };

            StartScanCommand = new RelayCommand(async () => await StartScan(), () => CanStart);
            PauseScanCommand = new RelayCommand(() => PauseScan(), () => CanPause);
            ResumeScanCommand = new RelayCommand(async () => await ResumeScan(), () => CanResume);
            StopScanCommand = new RelayCommand(() => StopScan(), () => CanStop);
            ClearResultsCommand = new RelayCommand(ClearResults, () => FilteredResults.Count > 0);
            ExportCommand = new RelayCommand(ExportResults);
            SaveSettingsCommand = new RelayCommand(SaveSettings);
            ShowHistoryCommand = new RelayCommand(ShowHistory);
            ShowFaqCommand = new RelayCommand(ShowFaq);
            OpenInBrowserCommand = new RelayCommand(OpenInBrowser);
            RunExternalScriptCommand = new RelayCommand(RunExternalScript);
            LoadHistoryCommand = new RelayCommand<ScanHistoryItem>(LoadHistory);
        }

        private void FlushUiBuffer()
        {
            foreach (var vm in _resultBuffer)
            {
                if (DoesMatchFilters(vm.Result))
                {
                    FilteredResults.Add(vm);
                }
                LogLines.Add($"Получен результат: {vm.Result.Ip}:{vm.Result.Port} ({vm.Result.ServiceType})");
            }
            _resultBuffer.Clear();
        }

        public void SetTabControl(TabControl tabControl)
        {
            _tabControl = tabControl;
        }

        private async Task StartScan()
        {
            if (!_networkGuard.IsSafeToScan(ScanSettings.IpRanges))
            {
                MessageBox.Show("Сканирование внешних сетей запрещено!");
                LogHelper.Log("Попытка сканирования внешней сети");
                return;
            }

            if (string.IsNullOrWhiteSpace(ScanSettings.IpRanges) || string.IsNullOrWhiteSpace(ScanSettings.Ports))
            {
                MessageBox.Show("Ошибка: Укажите IP-диапазоны и порты.");
                LogHelper.Log("Ошибка: Пустые IP-диапазоны или порты.");
                return;
            }

            CanStart = false;
            CanPause = true;
            CanStop = true;
            Status = "Сканирование начато...";
            FilteredResults.Clear();
            LogLines.Clear();
            _allScanResults.Clear();
            _resultBuffer.Clear();

            try
            {
                var historyItem = new ScanHistoryItem
                {
                    ScanDate = DateTime.Now,
                    Summary = $"Скан {ScanSettings.IpRanges}:{ScanSettings.Ports}",
                    FilePath = $"scan_{DateTime.Now:yyyyMMdd_HHmmss}.json"
                };
                await _scannerService.StartScanAsync(ScanSettings);
                _uiUpdateTimer.Stop();
                _chartUpdateTimer.Stop();
                FlushUiBuffer();
                CategoryHtmlAppender.FlushAll();
                Statistics.UpdateChart(null, FilteredResults.Select(r => r.Result));
                Statistics.Summary = $"Найдено результатов: {FilteredResults.Count}";
                historyItem.Results = _allScanResults.Select(vm => vm.Result).ToList();
                _historyService.SaveHistory(historyItem);
                ScanHistory.Add(historyItem);
                Status = "Сканирование завершено";
            }
            catch (Exception ex)
            {
                LogHelper.LogError(ex);
                Status = $"Ошибка при сканировании: {ex.Message}";
            }
            finally
            {
                _uiUpdateTimer.Stop();
                _chartUpdateTimer.Stop();
                FlushUiBuffer();
                CategoryHtmlAppender.FlushAll();
                CanStart = true;
                CanPause = false;
                CanStop = false;
            }
        }

        private void PauseScan()
        {
            _scannerService.Pause();
            CanPause = false;
            CanResume = true;
            Status = "Сканирование приостановлено";
            LogHelper.Log("Сканирование приостановлено в UI.");
            _uiUpdateTimer.Stop();
            FlushUiBuffer();
            CategoryHtmlAppender.FlushAll();
        }

        private async Task ResumeScan()
        {
            CanResume = false;
            CanPause = true;
            Status = "Сканирование возобновлено...";
            try
            {
                await _scannerService.Resume(ScanSettings);
                _uiUpdateTimer.Stop();
                _chartUpdateTimer.Stop();
                FlushUiBuffer();
                CategoryHtmlAppender.FlushAll();
                Statistics.UpdateChart(null, FilteredResults.Select(r => r.Result));
                Statistics.Summary = $"Найдено результатов: {FilteredResults.Count}";
                Status = "Сканирование завершено";
                CanStart = true;
                CanPause = false;
                CanStop = false;
            }
            catch (Exception ex)
            {
                LogHelper.LogError(ex);
                Status = $"Ошибка при возобновлении: {ex.Message}";
                CanStart = true;
                CanPause = false;
                CanResume = false;
                CanStop = false;
            }
        }

        private void StopScan()
        {
            _scannerService.Stop();
            CanStart = true;
            CanPause = false;
            CanResume = false;
            CanStop = false;
            Status = "Сканирование остановлено";
            _uiUpdateTimer.Stop();
            _chartUpdateTimer.Stop();
            FlushUiBuffer();
            CategoryHtmlAppender.FlushAll();
        }

        private void ClearResults()
        {
            _allScanResults.Clear();
            _resultBuffer.Clear();
            FilteredResults.Clear();
            LogLines.Clear();
            Statistics.Summary = "Нет данных о статистике.";
            Statistics.UpdateChart(null, new List<ScanResult>());
        }

        private void ExportResults()
        {
            var results = FilteredResults.Select(r => r.Result);
            _reportService.ExportToCsv(results, $"scan_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            _reportService.ExportToTxt(results, $"scan_export_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            _reportService.ExportToHtml(results, $"scan_export_{DateTime.Now:yyyyMMdd_HHmmss}.html");
            MessageBox.Show("Результаты экспортированы в CSV, TXT и HTML");
            LogHelper.Log("Результаты экспортированы");
        }

        private void SaveSettings()
        {
            var json = System.Text.Json.JsonSerializer.Serialize(ScanSettings, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText("settings.json", json);
            MessageBox.Show("Настройки сохранены в settings.json");
            LogHelper.Log("Настройки сохранены");
        }

        private void ShowHistory()
        {
            if (_tabControl != null)
            {
                _tabControl.SelectedIndex = 2;
            }
            LogHelper.Log("Открыта вкладка История");
        }

        private void ShowFaq()
        {
            if (_tabControl != null)
            {
                _tabControl.SelectedIndex = 3;
            }
            LogHelper.Log("Открыта вкладка FAQ");
        }

        private void OpenInBrowser()
        {
            var selected = FilteredResults.FirstOrDefault();
            if (selected != null)
            {
                var url = $"http://{selected.Result.Ip}:{selected.Result.Port}";
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
                LogHelper.Log($"Открыт в браузере: {url}");
            }
        }

        private void RunExternalScript()
        {
            var selected = FilteredResults.FirstOrDefault();
            if (selected != null)
            {
                LogHelper.Log($"Запущен скрипт для {selected.Result.Ip}:{selected.Result.Port}");
            }
        }

        private void LoadHistory(ScanHistoryItem item)
        {
            if (item != null)
            {
                _allScanResults.Clear();
                FilteredResults.Clear();
                foreach (var result in item.Results)
                {
                    _allScanResults.Add(new ScanResultViewModel { Result = result });
                }
                ApplyFilters();
                LogLines.Add($"Загружена история: {item.Summary}");
                Statistics.UpdateChart(null, item.Results);
                Statistics.Summary = $"Загружено результатов из истории: {item.Results.Count}";
                LogHelper.Log($"Загружена история: {item.Summary}");
            }
        }

        private bool DoesMatchFilters(ScanResult result)
        {
            if (!string.IsNullOrEmpty(FilterText) &&
                !result.Ip.Contains(FilterText, StringComparison.OrdinalIgnoreCase) &&
                !result.ServiceType.Contains(FilterText, StringComparison.OrdinalIgnoreCase))
                return false;

            if (ShowOnlyVulnerable && string.IsNullOrEmpty(result.Vulnerability))
                return false;

            if (ShowOnlyNew && !result.IsNew)
                return false;

            if (!string.IsNullOrEmpty(SelectedServiceType) && result.ServiceType != SelectedServiceType)
                return false;

            return true;
        }

        private void ApplyFilters()
        {
            FilteredResults.Clear();
            var filtered = _allScanResults
                .Where(vm => DoesMatchFilters(vm.Result))
                .OrderBy(vm => vm.Result.Ip)
                .ThenBy(vm => vm.Result.Port)
                .ToList();

            foreach (var item in filtered)
            {
                FilteredResults.Add(item);
            }

            Statistics.UpdateChart(null, FilteredResults.Select(r => r.Result));
            Statistics.Summary = $"Найдено результатов: {FilteredResults.Count}";
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;
        public void Execute(object parameter) => _execute();
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action<T> execute, Func<bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;
        public void Execute(object parameter) => _execute((T)parameter);
    }
}