using System;
using System.IO;

namespace NescaWpf.Helpers
{
    public static class LogHelper
    {
        private static readonly object _lock = new object();
        private static string _logFilePath = "scan_log.txt";

        public static void Log(string message)
        {
            try
            {
                lock (_lock)
                {
                    string logMessage = $"{DateTime.Now:dd.MM.yyyy HH:mm:ss}: {message}";
                    File.AppendAllText(_logFilePath, logMessage + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                // Игнорируем ошибки записи лога, чтобы не прерывать основную работу
                Console.WriteLine($"Ошибка записи лога: {ex.Message}");
            }
        }

        public static void LogError(Exception ex)
        {
            try
            {
                lock (_lock)
                {
                    string logMessage = $"{DateTime.Now:dd.MM.yyyy HH:mm:ss}: ERROR: {ex.Message}";
                    if (ex.StackTrace != null)
                    {
                        logMessage += $"\nStackTrace: {ex.StackTrace}";
                    }
                    File.AppendAllText(_logFilePath, logMessage + Environment.NewLine);
                }
            }
            catch (Exception innerEx)
            {
                Console.WriteLine($"Ошибка записи ошибки в лог: {innerEx.Message}");
            }
        }
    }
}