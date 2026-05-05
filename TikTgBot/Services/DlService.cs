using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TikTgBot.Services
{
    public class DlService(IServiceProvider _serviceProvider) : IDlService
    {
        public async Task<byte[]?> GetVideo<T>(string url, ServiceType serviceType, CancellationTokenSource cts)
            where T : class, IDlService
        {
            var service = _serviceProvider.GetService<T>();
            return service == null
                ? throw new InvalidOperationException($"Service {typeof(T).Name} not found.")
                : await service.GetVideo<T>(url, serviceType, cts);
        }
    }
}