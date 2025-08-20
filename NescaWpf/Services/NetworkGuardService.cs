namespace NescaWpf.Services
{
    public class NetworkGuardService
    {
        public bool IsSafeToScan(string ipRange)
        {
            // Разрешено сканирование любых сетей, включая внешние
            return true;
        }
    }
}