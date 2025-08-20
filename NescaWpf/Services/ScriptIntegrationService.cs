using System.Diagnostics;
using NescaWpf.Models;

namespace NescaWpf.Services
{
    public class ScriptIntegrationService
    {
        public void RunScript(ScanResult result, string scriptPath)
        {
            if (!string.IsNullOrEmpty(scriptPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = $"{scriptPath} {result.Ip} {result.Port}",
                    UseShellExecute = true
                });
            }
        }
    }
}