using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Moesocks.Client.Services.Network
{
    class WebProxyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly WebProxyProvider _proxyProvider;

        public WebProxyMiddleware(RequestDelegate next, WebProxyProvider proxyProvider)
        {
            _next = next;
            _proxyProvider = proxyProvider;
        }

        public async Task Invoke(HttpContext context)
        {
            await _proxyProvider.ProcessRequest(context);
            //await _next(context);
        }
    }
}
