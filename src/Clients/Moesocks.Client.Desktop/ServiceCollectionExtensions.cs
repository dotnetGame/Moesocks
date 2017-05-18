using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moesocks.Client.Configuration;
using Moesocks.Client.Services.Configuration;
using Moesocks.Client.Services.Network;
using System;
using System.Collections.Generic;
using System.Text;

namespace Moesocks.Client
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAppConfiguration(this IServiceCollection services, string fileName)
        {
            return services.AddSingleton(s => new AppConfiguration(fileName))
                .AddTransient<IOptions<UpdateConfiguration>>(s => s.GetRequiredService<AppConfiguration>().Update);
        }
    }
}
