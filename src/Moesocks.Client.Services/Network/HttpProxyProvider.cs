using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using Moesocks.Client.Services.Security;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using System.IO;

namespace Moesocks.Client.Services.Network
{
    class HttpProxyProvider
    {
        private readonly HttpProxySettings _settings;
        private readonly IMessageBus _messageBus;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private readonly TcpListener _listener;
        private int _eventId;

        public HttpProxyProvider(HttpProxySettings settings, IMessageBus messageBus, ILoggerFactory loggerFactory)
        {
            _settings = settings;
            _messageBus = messageBus;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<HttpProxyProvider>();
            _listener = new TcpListener(IPAddress.Parse(settings.LocalIPAddress), settings.LocalPort);
        }

        public async Task Startup(CancellationToken token)
        {
            try
            {
                _listener.Start();
                await Task.Run(async () =>
                {
                    while (true)
                    {
                        token.ThrowIfCancellationRequested();
                        try
                        {
                            DispatchIncoming(await _listener.AcceptTcpClientAsync());
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(Interlocked.Increment(ref _eventId), ex, ex.Message);
                        }
                    }
                }, token);
            }
            finally
            {
                _listener.Stop();
            }
        }

        private async void DispatchIncoming(TcpClient tcpClient)
        {
            try
            {
                using (tcpClient)
                {
                    var buffer = new byte[512];
                    var stream = tcpClient.GetStream();
                    var httpParser = new HttpParser(stream, buffer);
                    var takenStream = new MemoryStream();
                    await httpParser.Parse(takenStream);

                    (var host, var port) = ParseHostAndPort(httpParser.Host);
                    if (httpParser.Method == "CONNECT")
                    {
                        _logger.LogInformation($"Tunnel to: {httpParser.Host}");
                        var session = new TunnelProxySession(host, port, tcpClient.Client, stream, takenStream.ToArray(), _messageBus, _loggerFactory);
                        await session.Run();
                    }
                    else
                    {
                        _logger.LogInformation($"Http request to: {httpParser.Host}");
                        var session = new HttpProxySession(host, port, stream, takenStream.ToArray(), _messageBus, _loggerFactory);
                        await session.Run();
                    }
                }
            }
            catch(InvalidDataException)
            {

            }
            catch (Exception ex)
            {
                _logger.LogError(Interlocked.Increment(ref _eventId), ex, ex.Message);
            }
        }

        private (string host, ushort port) ParseHostAndPort(string targetHost)
        {
            var idx = targetHost.IndexOf(':');
            if (idx != -1)
                return (targetHost.Substring(0, idx).Trim(), ushort.Parse(targetHost.Substring(idx + 1)));
            else
                return (targetHost.Trim(), 443);
        }

        class HttpParser
        {
            private readonly Stream _remoteStream;
            private readonly byte[] _buffer;
            public string Method { get; private set; }
            public string Host { get; private set; }

            public HttpParser(Stream remoteStream, byte[] buffer)
            {
                _remoteStream = remoteStream;
                _buffer = buffer;
            }

            enum State
            {
                FillingMethod,
                IgnoringLine,
                FillingHeader,
                FillingHeaderValue,
                Done
            }

            private const byte ByteLF = (byte)'\n';
            private const byte ByteCR = (byte)'\r';
            private const long MaxTempHeaderLength = 1024 * 64; // 64 KiB

            public async Task Parse(Stream remoteTaken)
            {
                State state = State.FillingMethod;
                int hostHeaderIdx = 0, methodIdx = 0;
                var hostHeader = new StringBuilder();
                var method = new StringBuilder();
                while (true)
                {
                    var read = await _remoteStream.ReadAsync(_buffer, 0, _buffer.Length);
                    if (read == 0)
                        throw new InvalidDataException();
                    await remoteTaken.WriteAsync(_buffer, 0, read);
                    MoveState(_buffer, read, ref state, ref methodIdx, ref hostHeaderIdx, method, hostHeader);
                    if (state == State.Done)
                    {
                        Method = method.ToString();
                        Host = hostHeader.ToString();
                        return;
                    }
                    else if (remoteTaken.Length > MaxTempHeaderLength)
                        throw new InvalidDataException();
                }
            }

            private const string _hostHeader = "host: ";

            private void MoveState(byte[] buffer, int length, ref State state, ref int methodIdx, ref int hostHeaderIdx, StringBuilder method, StringBuilder hostHeader)
            {
                int cntIdx = 0;
                while (true)
                {
                    if (cntIdx >= length) break;
                    switch (state)
                    {
                        case State.FillingMethod:
                            {
                                for (int i = methodIdx; cntIdx < length; i++)
                                {
                                    var c = (char)buffer[cntIdx++];
                                    if (c == ' ')
                                    {
                                        state = State.IgnoringLine;
                                        break;
                                    }
                                    else if (char.IsLetter(c))
                                        method.Append(c);
                                    else
                                        throw new InvalidDataException();
                                    methodIdx++;
                                }
                            }
                            break;
                        case State.IgnoringLine:
                            {
                                var idx = Array.IndexOf(_buffer, ByteLF, cntIdx, length - cntIdx);
                                if (idx != -1)
                                {
                                    cntIdx = idx + 1;
                                    hostHeaderIdx = 0;
                                    state = State.FillingHeader;
                                }
                                else
                                    break;
                            }
                            break;
                        case State.FillingHeader:
                            {
                                for (int i = hostHeaderIdx; i < _hostHeader.Length && cntIdx < length; i++)
                                {
                                    var c = (char)buffer[cntIdx++];
                                    if (char.ToLower(c) != _hostHeader[i])
                                    {
                                        state = State.IgnoringLine;
                                        break;
                                    }
                                    hostHeaderIdx++;
                                }
                                if (hostHeaderIdx == _hostHeader.Length)
                                    state = State.FillingHeaderValue;
                            }
                            break;
                        case State.FillingHeaderValue:
                            {
                                while (cntIdx < length)
                                {
                                    var c = buffer[cntIdx++];
                                    if (c != 0 && c != ByteCR && c != ByteLF)
                                        hostHeader.Append((char)c);
                                    else
                                    {
                                        state = State.Done;
                                        break;
                                    }
                                }
                            }
                            break;
                        default:
                            return;
                    }
                }
            }
        }
    }
}
