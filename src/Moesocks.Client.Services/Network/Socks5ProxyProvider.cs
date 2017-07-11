using Microsoft.Extensions.Logging;
using Moesocks.Socks5;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using Moesocks.Socks5.Protocol.Messages;
using System.Threading.Tasks;

namespace Moesocks.Client.Services.Network
{
    class Socks5ProxyProvider : Socks5ServerBase
    {
        private readonly IMessageBus _messageBus;

        public Socks5ProxyProvider(IMessageBus messageBus, ILoggerFactory loggerFactory)
            :base(new IPEndPoint(IPAddress.Any, 1080), loggerFactory)
        {
            _messageBus = messageBus;
        }

        protected override Socks5ProxySessionBase CreateSession(Stream remoteStream)
        {
            return new Socks5ProxySession(remoteStream, _messageBus, _loggerFactory);
        }

        class Socks5ProxySession : Socks5ProxySessionBase
        {
            private readonly IMessageBus _messageBus;
            private readonly ILoggerFactory _loggerFactory;
            private TunnelProxySession _tunnelProxySession;

            public Socks5ProxySession(Stream remoteStream, IMessageBus messageBus, ILoggerFactory loggerFactory) 
                : base(remoteStream)
            {
                _messageBus = messageBus;
                _loggerFactory = loggerFactory;
            }

            protected override Task ConnectAsync(AddressType addressType, DnsEndPoint dest)
            {
                _tunnelProxySession = new TunnelProxySession(dest.Host, (ushort)dest.Port, RemoteStream, Array.Empty<byte>(), _messageBus, _loggerFactory);
                return Task.CompletedTask;
            }

            protected override async Task RunTunnel()
            {
                await _tunnelProxySession.Run(false);
            }
        }
    }
}
