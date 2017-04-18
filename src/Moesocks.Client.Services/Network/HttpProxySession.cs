using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.IO;

namespace Moesocks.Client.Services.Network
{
    class HttpProxySession
    {
        private readonly Stream _remoteStream;
        private readonly IMessageBus _messageBus;
        private readonly ILoggerFactory _loggerFactory;
        private readonly string _targetHost;
        private readonly ushort _targetPort;
        private uint _identifier;
        private readonly uint _sessionKey;

        public HttpProxySession(string targetHost, Stream remoteStream, byte[] remoteTaken, IMessageBus messageBus, ILoggerFactory loggerFactory)
        {
            (_targetHost, _targetPort) = ParseHostAndPort(targetHost);
            _remoteStream = remoteStream;
            _messageBus = messageBus;
            _loggerFactory = loggerFactory;
            _sessionKey = (uint)this.GetHashCode();
        }

        private (string host, ushort port) ParseHostAndPort(string targetHost)
        {
            var idx = targetHost.IndexOf(':');
            if (idx != -1)
                return (targetHost.Substring(0, idx).Trim(), ushort.Parse(targetHost.Substring(idx + 1)));
            else
                return (targetHost.Trim(), 443);
        }

        public async Task Run()
        {

        }
    }
}
