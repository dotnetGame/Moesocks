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
            var address = IPAddress.Parse("192.168.0.1");
            var ipBuilder = new IPPacketBuilder
            {
                Source = IPAddress.Parse("192.168.0.77"),
                Destination = IPAddress.Parse("192.168.0.255"),
                Id = 0x5AC7,
                Protocol = 0x11,
                TTL = 0x80,
                Payload = new ArraySegment<byte>(new byte[58])
            };
            var packetLength = ipBuilder.Build(new ArraySegment<byte>(_buffer));
            var count = await _client.SendToAsync(new ArraySegment<byte>(_buffer, 0, packetLength), SocketFlags.None, _serverEndPoint);
            using (var bw = File.OpenWrite($"{DateTime.Now.TimeOfDay.ToString("hh\\-mm\\-ss")}.bin"))
                await bw.WriteAsync(_buffer, 0, packetLength);
            _logger.LogInformation($"{count} bytes sent.");
        }
    }
}
