using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Moesocks.Server.Services.Network
{
    class ConnectionRouter : IConnectionRouter
    {
        private readonly Socket _listener;
        private readonly ILogger _logger;

        public ConnectionRouter(IOptions<ConnectionRouterSettings> settings, ILoggerFactory loggerFactory)
        {
            _listener = CreateListener(settings.Value);
            _logger = loggerFactory.CreateLogger<ConnectionRouter>();
        }
        const int SIO_RCVALL = unchecked((int)0x98000001);
        private Socket CreateListener(ConnectionRouterSettings settings)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP);
            socket.Bind(new IPEndPoint(IPAddress.Parse(settings.ServerIPAddress), settings.ServerPort));
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, 1);
            return socket;
        }

        public async void Startup()
        {
            while (true)
            {
                var buffer = new ArraySegment<byte>(new byte[4096]);
                var result = await _listener.ReceiveFromAsync(buffer, SocketFlags.None, _listener.LocalEndPoint);
                var remote = (IPEndPoint)result.RemoteEndPoint;
                using (var bw = File.OpenWrite($"{DateTime.Now.TimeOfDay.ToString("hh\\-mm\\-ss")}.bin"))
                    await bw.WriteAsync(buffer.Array, 0, result.ReceivedBytes);
                _logger.LogInformation($"{result.ReceivedBytes} bytes received from {remote}.");
            }
        }
    }
}
