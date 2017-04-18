using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moesocks.Protocol.Messages;

namespace Moesocks.Client.Services.Network
{
    class TunnelProxySession
    {
        private readonly Stream _remoteStream;
        private readonly IMessageBus _messageBus;
        private readonly ILoggerFactory _loggerFactory;
        private readonly string _targetHost;
        private readonly ushort _targetPort;
        private uint _identifier;
        private readonly uint _sessionKey;

        public TunnelProxySession(string targetHost, Stream remoteStream, byte[] takenBytes, IMessageBus messageBus, ILoggerFactory loggerFactory)
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
            var tasks = new[] { RunClientRead(), RunMessageReceive() };
            await Task.WhenAll(tasks);
        }

        private async Task RunMessageReceive()
        {
            while (true)
            {
                (var identifier, var message) = await _messageBus.ReceiveAsync(_sessionKey);
                switch (message)
                {
                    case TcpContentMessage contentMsg:
                        await _remoteStream.WriteAsync(contentMsg.Content, 0, contentMsg.Content.Length);
                        break;
                    case TcpEndOfFileMessage _:
                        await _remoteStream.FlushAsync();
                        _remoteStream.Dispose();
                        break;
                    default:
                        throw new InvalidOperationException("unrecognizable message.");
                }
            }
        }

        private async Task RunClientRead()
        {
            var buffer = new byte[4096];
            while (true)
            {
                var read = await _remoteStream.ReadAsync(buffer, 0, buffer.Length);
                if (read == 0)
                {
                    await _messageBus.SendAsync(_sessionKey, _identifier++, new TcpEndOfFileMessage());
                    break;
                }
                else
                {
                    var dst = new byte[read];
                    Array.Copy(buffer, dst, read);
                    await _messageBus.SendAsync(_sessionKey, _identifier++, new TcpContentMessage
                    {
                        Host = _targetHost,
                        Port = _targetPort,
                        Content = dst
                    });
                }
            }
        }
    }
}
