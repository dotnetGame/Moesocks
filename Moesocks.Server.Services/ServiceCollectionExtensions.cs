using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moesocks.Server.Services;
using Moesocks.Server.Services.Network;
using System;
using System.Collections.Generic;
using System.Text;

namespace Moesocks.Server
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddConnectionRouter(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<ConnectionRouterSettings>(configuration);
            return services.AddScoped<IConnectionRouter, ConnectionRouter>();
        }
    }
}
