using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moesocks.Client.Services.Network;
using System;
using System.Collections.Generic;
using System.Text;

namespace Moesocks.Client
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddConnectionRouter(this IServiceCollection services, IConfiguration configuration)
        {
            return services.Configure<ConnectionRouterSettings>(configuration)
                .AddSingleton<IConnectionRouter, ConnectionRouter>();
        }

        public static IServiceCollection AddSecurity(this IServiceCollection services, IConfiguration configuration)
        {
            return services.Configure<SecuritySettings>(configuration);
        }
    }
}
