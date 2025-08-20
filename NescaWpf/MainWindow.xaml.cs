using System.Windows;
using System.Windows.Controls;
using NescaWpf.ViewModels;

namespace NescaWpf
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
            Loaded += (s, e) =>
            {
                var vm = (MainViewModel)DataContext;
                vm.SetTabControl(MainTabControl); // Для переключения вкладок
                vm.Statistics.UpdateChart(StatsWebBrowser); // Инициализация графика
            };
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}