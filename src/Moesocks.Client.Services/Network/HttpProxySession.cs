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

        public HttpProxySession(string targetHost, ushort targetPort, Stream remoteStream, byte[] remoteTaken, IMessageBus messageBus, ILoggerFactory loggerFactory)
        {
            _targetHost = targetHost;
            _targetPort = targetPort;
            _remoteStream = remoteStream;
            _messageBus = messageBus;
            _loggerFactory = loggerFactory;
            _sessionKey = (uint)this.GetHashCode();
        }

        public async Task Run()
        {

        }
    }
}
