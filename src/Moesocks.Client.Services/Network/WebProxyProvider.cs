using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Net.Sockets;
using Microsoft.Extensions.Options;
using System.Net;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading;

namespace Moesocks.Client.Services.Network
{
    class WebProxyProvider
    {
        private readonly Socket _client;
        private readonly IPEndPoint _serverEndPoint;
        private readonly ILogger _logger;
        private ushort _packetId;
        private readonly byte[] _buffer = new byte[1024 * 64];

        public WebProxyProvider(IOptions<WebProxySettings> settings, ILoggerFactory loggerFactory)
        {
            _client = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP);
            _client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, 1);
            _serverEndPoint = new IPEndPoint(IPAddress.Parse(settings.Value.ServerIPAddress), settings.Value.ServerPort);
            _logger = loggerFactory.CreateLogger(typeof(WebProxyProvider));
        }

        public async Task ProcessRequest(HttpContext context)
        {
            var request = context.Request;
        }
    }
}
