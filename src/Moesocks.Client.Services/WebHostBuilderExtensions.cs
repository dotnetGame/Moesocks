using Microsoft.AspNetCore.Hosting;
using System;

namespace Moesocks.Client
{
    public static class WebHostBuilderExtensions
    {
        public static IWebHostBuilder UseWebProxy(this IWebHostBuilder builder)
        {
            return builder.UseKestrel(o =>
            {
                o.NoDelay = true;
                o.UseConnectionLogging();
            })
            .UseUrls("http://localhost:5000");
        }
    }
}
