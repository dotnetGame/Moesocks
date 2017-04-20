using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moesocks.Protocol.Messages;
using System.Linq;

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
        private byte[] _takenBytes;

        public TunnelProxySession(string targetHost, Stream remoteStream, byte[] takenBytes, IMessageBus messageBus, ILoggerFactory loggerFactory)
        {
            (_targetHost, _targetPort) = ParseHostAndPort(targetHost);
            _remoteStream = remoteStream;
            _messageBus = messageBus;
            _loggerFactory = loggerFactory;
            _sessionKey = (uint)this.GetHashCode();
            _takenBytes = takenBytes;
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
            await SendOkResponse();
            var tasks = new[] { RunMessageReceive(), RunClientRead() };
            await Task.WhenAll(tasks);
        }

        private const string _okResponse = "HTTP/1.1 200 OK\r\n\r\n";
        private static readonly byte[] _okResponseBytes = Encoding.ASCII.GetBytes(_okResponse);

        private async Task SendOkResponse()
        {
            var eor = FindEndOfRequest(_takenBytes, _takenBytes.Length);
            if(eor == -1)
            {
                var buffer = new byte[512];
                int read = 0;
                while(eor == -1)
                {
                    read = await _remoteStream.ReadAsync(buffer, 0, buffer.Length);
                    if (read == 0)
                        throw new InvalidDataException();
                    eor = FindEndOfRequest(buffer, read);
                }
                _takenBytes = buffer.Skip(eor + 4).Take(read - eor - 4).ToArray();
            }
            else
                _takenBytes = _takenBytes.Skip(eor + 4).Take(_takenBytes.Length - eor - 4).ToArray();
            await _remoteStream.WriteAsync(_okResponseBytes, 0, _okResponseBytes.Length);
        }

        private static readonly byte[] _eor = new[] { (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n' };
        static int FindEndOfRequest(byte[] content, int length)
        {
            int start = 0;
            while (start + _eor.Length <= length)
            {
                var first = Array.IndexOf(content, _eor[0], start);
                if (first == -1 || first + _eor.Length > length) return -1;

                bool next = false;
                for (int i = 0; i < _eor.Length; i++)
                {
                    if(content[first + i] != _eor[i])
                    {
                        next = true;
                        break;
                    }
                }
                if (!next)
                    return first;
                else
                    start = first + 1;
            }
            return -1;
        }

        private async Task RunMessageReceive()
        {
            var completionSource = new TaskCompletionSource<object>();
            _messageBus.BeginReceive(_sessionKey, async (identifier, message) =>
            {
                switch (message)
                {
                    case TcpContentMessage contentMsg:
                        await _remoteStream.WriteAsync(contentMsg.Content, 0, contentMsg.Content.Length);
                        break;
                    case TcpEndOfFileMessage _:
                        _messageBus.EndReceive(_sessionKey);
                        await _remoteStream.FlushAsync();
                        _remoteStream.Dispose();
                        completionSource.SetResult(null);
                        break;
                    default:
                        throw new InvalidOperationException("unrecognizable message.");
                }
            });
            await completionSource.Task;
        }

        private async Task RunClientRead()
        {
            if(_takenBytes.Length != 0)
            {
                await _messageBus.SendAsync(_sessionKey, _identifier++, new TcpContentMessage
                {
                    Host = _targetHost,
                    Port = _targetPort,
                    Content = _takenBytes
                });
            }

            var buffer = new byte[4096];
            while (true)
            {
                var read = await _remoteStream.ReadAsync(buffer, 0, buffer.Length);
                if (read == 0)
                {
                    await _messageBus.SendAsync(_sessionKey, _identifier++, new TcpEndOfFileMessage
                    {
                        Host = _targetHost,
                        Port = _targetPort
                    });
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
