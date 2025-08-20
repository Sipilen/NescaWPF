using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using NescaWpf.Models;

namespace NescaWpf.Services
{
    public class StatisticsService
    {
        public void UpdateChart(WebBrowser webBrowser, IEnumerable<ScanResult> results)
        {
            var serviceCounts = results
                .GroupBy(r => r.ServiceType)
                .Select(g => new { Service = g.Key, Count = g.Count() })
                .ToList();

            var labels = serviceCounts.Select(s => s.Service).ToArray();
            var data = serviceCounts.Select(s => s.Count).ToArray();

            var html = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <script src='https://cdn.jsdelivr.net/npm/chart.js'></script>
                </head>
                <body>
                    <canvas id='statsChart'></canvas>
                    <script>
                        var ctx = document.getElementById('statsChart').getContext('2d');
                        new Chart(ctx, {{
                            type: 'bar',
                            data: {{
                                labels: {System.Text.Json.JsonSerializer.Serialize(labels)},
                                datasets: [{{
                                    label: 'Количество сервисов',
                                    data: {System.Text.Json.JsonSerializer.Serialize(data)},
                                    backgroundColor: ['#FF6384', '#36A2EB', '#FFCE56', '#4BC0C0', '#9966FF']
                                }}]
                            }},
                            options: {{
                                scales: {{
                                    y: {{ beginAtZero: true }}
                                }}
                            }}
                        }});
                    </script>
                </body>
                </html>";

            webBrowser.NavigateToString(html);
        }
    }
}