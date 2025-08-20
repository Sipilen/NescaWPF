using System.Collections.Generic;

namespace NescaWpf.Services
{
    public class PresetService
    {
        public Dictionary<string, string> GetPortPresets()
        {
            return new Dictionary<string, string>
            {
                { "Web (80,443,8080,8000,8443)", "80,443,8080,8000,8443" },
                { "FTP (21,2121)", "21,2121" },
                { "Camera (554,37777,34567)", "554,37777,34567" },
                { "IoT/SCADA", "502,1883,44818" },
                { "Все (1-1024)", "1-1024" }
            };
        }
    }
}