using Microsoft.AspNetCore.Builder;
using Moesocks.Client.Services.Network;
using System;
using System.Collections.Generic;
using System.Text;

namespace Moesocks.Client
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseWebProxy(this IApplicationBuilder app)
        {
            return app.UseMiddleware<WebProxyMiddleware>();
        }
    }
}
