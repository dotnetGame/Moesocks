using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moesocks.Protocol.Messages;
using System.Linq;
using System.Threading;

namespace Moesocks.Client.Services.Network
{
    class TunnelProxySession
    {
        private readonly Socket _socket;
        private readonly Stream _remoteStream;
        private readonly IMessageBus _messageBus;
        private readonly ILoggerFactory _loggerFactory;
        private readonly string _targetHost;
        private readonly ushort _targetPort;
        private uint _identifier;
        private readonly uint _sessionKey;
        private byte[] _takenBytes;
        private TaskCompletionSource<object> _receiveTcs;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public TunnelProxySession(string targetHost, ushort targetPort, Socket socket, Stream remoteStream, byte[] takenBytes, IMessageBus messageBus, ILoggerFactory loggerFactory)
        {
            _targetHost = targetHost;
            _targetPort = targetPort;
            _socket = socket;
            _remoteStream = remoteStream;
            _messageBus = messageBus;
            _loggerFactory = loggerFactory;
            _sessionKey = (uint)this.GetHashCode();
            _takenBytes = takenBytes;
        }

        public async Task Run(bool isHttpTunnel = true)
        {
            try
            {
                if (isHttpTunnel)
                    await SendOkResponse();
                var tasks = new[] { RunMessageReceive(), RunClientRead() };
                await Task.WhenAll(tasks);
            }
            finally
            {
                _messageBus.EndReceive(_sessionKey);
            }
        }

        private const string _okResponse = "HTTP/1.1 200 OK\r\n\r\n";
        private static readonly byte[] _okResponseBytes = Encoding.ASCII.GetBytes(_okResponse);

        private async Task SendOkResponse()
        {
            var eor = FindEndOfRequest(_takenBytes, _takenBytes.Length);
            if (eor == -1)
            {
                var buffer = new byte[512];
                int read = 0;
                while (eor == -1)
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
                    if (content[first + i] != _eor[i])
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
            _receiveTcs = new TaskCompletionSource<object>();
            _messageBus.BeginReceive(_sessionKey, async (identifier, message) =>
            {
                try
                {
                    switch (message)
                    {
                        case TcpContentMessage contentMsg:
                            await _remoteStream.WriteAsync(contentMsg.Content, 0, contentMsg.Content.Length, _cancellationTokenSource.Token);
                            break;
                        case TcpEndOfFileMessage _:
                            _messageBus.EndReceive(_sessionKey);
                            await _remoteStream.FlushAsync();
                            _socket.Shutdown(SocketShutdown.Send);
                            _receiveTcs.SetResult(null);
                            break;
                        case TcpErrorMessage _:
                            OnError();
                            break;
                        default:
                            throw new InvalidOperationException("unrecognizable message.");
                    }
                }
                catch
                {
                    OnError();
                }
            });
            await _receiveTcs.Task;
        }

        private readonly byte[] _readBuffer = new byte[1024 * 16];

        private async void OnError()
        {
            try
            {
                await _messageBus.SendAsync(_sessionKey, _identifier++, new TcpErrorMessage(), () => { });
            }
            catch
            {
            }

            try
            {
                _messageBus.EndReceive(_sessionKey);
                _cancellationTokenSource.Cancel(false);
                _receiveTcs?.SetException(new TaskCanceledException());
                _socket.Close();
            }
            catch
            {
            }
        }

        private async Task RunClientRead()
        {
            if (_takenBytes.Length != 0)
            {
                await _messageBus.SendAsync(_sessionKey, _identifier++, new TcpContentMessage
                {
                    Host = _targetHost,
                    Port = _targetPort,
                    Content = _takenBytes
                }, OnError);
            }

            try
            {

                while (true)
                {
                    var read = await _remoteStream.ReadAsync(_readBuffer, 0, _readBuffer.Length);
                    if (read == 0)
                    {
                        await _messageBus.SendAsync(_sessionKey, _identifier++, new TcpEndOfFileMessage
                        {
                            Host = _targetHost,
                            Port = _targetPort
                        }, OnError);
                        break;
                    }
                    else
                    {
                        var dst = new byte[read];
                        Array.Copy(_readBuffer, dst, read);
                        await _messageBus.SendAsync(_sessionKey, _identifier++, new TcpContentMessage
                        {
                            Host = _targetHost,
                            Port = _targetPort,
                            Content = dst
                        }, OnError);
                    }
                }
            }
            catch
            {
                OnError();
            }
        }
    }
}
