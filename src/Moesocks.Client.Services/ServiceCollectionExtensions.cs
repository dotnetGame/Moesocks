using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moesocks.Client.Services.Network;
using System;
using System.Collections.Generic;
using System.Text;

namespace Moesocks.Client
{
    public class WebProxySettings : IOptions<WebProxySettings>
    {
        WebProxySettings IOptions<WebProxySettings>.Value => this;

        public string ServerAddress { get; set; }
        public int ServerPort { get; set; }
        public string ClientCertificateFileName { get; set; }
        public string ClientCertificatePassword { get; set; }
        public ushort MaxRandomBytesLength { get; set; }
    }

    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWebProxy(this IServiceCollection services, IConfiguration configuration)
        {
            return services.Configure<WebProxySettings>(configuration)
                .AddSingleton<WebProxyProvider>();
        }
    }
}
