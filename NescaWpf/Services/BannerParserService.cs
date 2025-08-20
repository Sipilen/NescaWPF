using System.Collections.Generic;
using System.Linq;
using NescaWpf.Models;

namespace NescaWpf.Services
{
    public class BannerParserService
    {
        private readonly List<ServiceBanner> _banners = new List<ServiceBanner>
        {
            new ServiceBanner { Banner = "HTTP/1.1", ServiceType = "HTTP" },
            new ServiceBanner { Banner = "SSH-2.0", ServiceType = "SSH" },
            new ServiceBanner { Banner = "220 FTP", ServiceType = "FTP" }
        };

        public string ParseBanner(string banner)
        {
            return _banners.FirstOrDefault(b => banner.Contains(b.Banner))?.ServiceType ?? "Other";
        }
    }
}