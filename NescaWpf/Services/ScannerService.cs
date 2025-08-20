using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NescaWpf.Models;
using NescaWpf.Helpers;

namespace NescaWpf.Services
{
    public class ScannerService
    {
        public event Action<ScanResult> OnResult;
        public event Action<int, int> OnProgress;
        private CancellationTokenSource _cts;
        private readonly BannerParserService _bannerParser;
        private readonly VulnerabilityCheckerService _vulnChecker;

        private string _ipRangesStr;
        private string _portsStr;
        private int _processedTasks = 0;
        private bool _isPaused = false;

        public ScannerService()
        {
            _bannerParser = new BannerParserService();
            _vulnChecker = new VulnerabilityCheckerService();
        }

        public async Task StartScanAsync(ScanSettings settings)
        {
            if (!_isPaused)
            {
                _ipRangesStr = settings.IpRanges;
                _portsStr = settings.Ports;
                _processedTasks = 0;
            }
            _isPaused = false;
            _cts = new CancellationTokenSource();
            LogHelper.Log($"Начало сканирования: IP {settings.IpRanges}, порты {settings.Ports}, потоков: {settings.Threads}");

            try
            {
                var ips = ParseIpRanges(_ipRangesStr);
                var ports = ParsePorts(_portsStr);
                int totalTasks = EstimateTotalTasks(_ipRangesStr, _portsStr);

                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = settings.Threads,
                    CancellationToken = _cts.Token
                };

                int localProcessed = _processedTasks;

                await Task.Run(() =>
                {
                    Parallel.ForEach(ips, parallelOptions, ip =>
                    {
                        foreach (var port in ports)
                        {
                            _cts.Token.ThrowIfCancellationRequested();
                            var result = ScanPortAsync(ip, port, settings).GetAwaiter().GetResult();
                            if (result != null)
                            {
                                result.ServiceType = _bannerParser.ParseBanner(result.Banner);
                                result.Vulnerability = _vulnChecker.CheckVulnerability(result);
                                OnResult?.Invoke(result);
                                LogHelper.Log($"Найден сервис: {ip}:{port} ({result.ServiceType})");
                            }
                            Interlocked.Increment(ref localProcessed);
                            OnProgress?.Invoke(localProcessed, totalTasks);
                        }
                    });
                });

                _processedTasks = localProcessed;
            }
            catch (OperationCanceledException)
            {
                _isPaused = true;
                LogHelper.Log("Сканирование приостановлено");
            }
            catch (Exception ex)
            {
                LogHelper.LogError(ex);
            }
            finally
            {
                if (!_isPaused)
                {
                    LogHelper.Log("Сканирование завершено.");
                }
            }
        }

        private async Task<ScanResult> ScanPortAsync(string ip, int port, ScanSettings settings)
        {
            try
            {
                if (settings.EnableTcp)
                {
                    using var client = new TcpClient();
                    client.ReceiveTimeout = settings.Timeout;
                    client.SendTimeout = settings.Timeout;

                    var connectTask = client.ConnectAsync(ip, port);
                    if (await Task.WhenAny(connectTask, Task.Delay(settings.Timeout, _cts.Token)) == connectTask)
                    {
                        if (client.Connected)
                        {
                            string banner = await GetBannerAsync(client, ip, port);
                            return new ScanResult
                            {
                                Ip = ip,
                                Port = port,
                                Protocol = "TCP",
                                Banner = banner,
                                Time = DateTime.Now,
                                IsNew = true
                            };
                        }
                    }
                }

                if (settings.EnableUdp)
                {
                    using var udpClient = new UdpClient();
                    udpClient.Client.ReceiveTimeout = settings.Timeout;
                    udpClient.Client.SendTimeout = settings.Timeout;

                    byte[] data = Encoding.ASCII.GetBytes("UDP probe");
                    await udpClient.SendAsync(data, data.Length, ip, port);

                    var receiveTask = udpClient.ReceiveAsync();
                    if (await Task.WhenAny(receiveTask, Task.Delay(settings.Timeout, _cts.Token)) == receiveTask)
                    {
                        var udpResult = await receiveTask;
                        string banner = Encoding.ASCII.GetString(udpResult.Buffer).Trim();
                        return new ScanResult
                        {
                            Ip = ip,
                            Port = port,
                            Protocol = "UDP",
                            Banner = banner,
                            Time = DateTime.Now,
                            IsNew = true
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogError(new Exception($"Ошибка при сканировании {ip}:{port}: {ex.Message}"));
            }
            return null;
        }

        private async Task<string> GetBannerAsync(TcpClient client, string ip, int port)
        {
            try
            {
                var stream = client.GetStream();
                if (port == 80 || port == 8080 || port == 443)
                {
                    string probe = $"HEAD / HTTP/1.1\r\nHost: {ip}\r\nConnection: close\r\n\r\n";
                    byte[] probeBytes = Encoding.ASCII.GetBytes(probe);
                    await stream.WriteAsync(probeBytes, 0, probeBytes.Length);
                }

                var buffer = new byte[4096];
                var readTask = stream.ReadAsync(buffer, 0, buffer.Length);
                if (await Task.WhenAny(readTask, Task.Delay(2000, _cts.Token)) == readTask)
                {
                    int bytesRead = await readTask;
                    string banner = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();

                    if (banner.Contains("<title>"))
                    {
                        int start = banner.IndexOf("<title>") + 7;
                        int end = banner.IndexOf("</title>", start);
                        if (end > start)
                        {
                            return banner.Substring(start, end - start).Trim() + " " + banner;
                        }
                    }
                    return banner;
                }
            }
            catch { }
            return string.Empty;
        }

        private IEnumerable<string> ParseIpRanges(string ipRanges)
        {
            if (string.IsNullOrWhiteSpace(ipRanges)) yield break;

            var ranges = ipRanges.Split(new[] { '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(r => r.Trim());

            foreach (var range in ranges)
            {
                if (range.Contains('-'))
                {
                    var parts = range.Split('-');
                    var startParts = parts[0].Split('.').Select(int.Parse).ToArray();
                    var endParts = parts[1].Split('.').Select(int.Parse).ToArray();

                    var current = startParts.ToArray();
                    while (true)
                    {
                        yield return string.Join(".", current);
                        if (!IncrementIp(current, endParts)) break;
                    }
                }
                else
                {
                    yield return range;
                }
            }
        }

        private bool IncrementIp(int[] current, int[] end)
        {
            current[3]++;
            for (int i = 3; i >= 0; i--)
            {
                if (current[i] > 255)
                {
                    current[i] = 0;
                    if (i > 0) current[i - 1]++;
                }
            }
            for (int i = 0; i < 4; i++)
            {
                if (current[i] > end[i]) return false;
                if (current[i] < end[i]) return true;
            }
            return true;
        }

        private IEnumerable<int> ParsePorts(string ports)
        {
            if (string.IsNullOrWhiteSpace(ports)) yield break;

            var portRanges = ports.Split(new[] { '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim());

            foreach (var portRange in portRanges)
            {
                if (portRange.Contains('-'))
                {
                    var parts = portRange.Split('-');
                    int start = int.Parse(parts[0]);
                    int end = int.Parse(parts[1]);
                    for (int i = Math.Max(1, start); i <= Math.Min(65535, end); i++)
                    {
                        yield return i;
                    }
                }
                else
                {
                    int port = int.Parse(portRange);
                    if (port >= 1 && port <= 65535) yield return port;
                }
            }
        }

        private int EstimateTotalTasks(string ipRanges, string ports)
        {
            return ParseIpRanges(ipRanges).Count() * ParsePorts(ports).Count();
        }

        public void Pause() { _cts?.Cancel(); LogHelper.Log("Сканирование приостановлено."); }
        public async Task Resume(ScanSettings settings) { await StartScanAsync(settings); }
        public void Stop() { _cts?.Cancel(); _isPaused = false; _processedTasks = 0; LogHelper.Log("Сканирование остановлено."); }
    }
}