using Microsoft.Extensions.Logging;
using Moesocks.Protocol;
using Moesocks.Protocol.Messages;
using Moesocks.Server.Services.Security;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tomato.Threading;

namespace Moesocks.Server.Services.Network
{
    class ProxySession
    {
        private readonly SecureTransportSession _transport;
        private readonly ILogger _logger;
        private readonly MessageSerializer _messageSerializer;
        private readonly ConcurrentDictionary<TcpClientKey, TcpClient> _tcpClients = new ConcurrentDictionary<TcpClientKey, TcpClient>();
        private readonly OperationQueue _responseDispatcher = new OperationQueue(1);
        private uint _identifier;

        public ProxySession(SecureTransportSession transport, ILoggerFactory loggerFactory)
        {
            _transport = transport;
            _logger = loggerFactory.CreateLogger<ProxySession>();
            _messageSerializer = new MessageSerializer();
        }

        public Task Run(CancellationToken token)
        {
            return Task.Run(async () =>
            {
                token.ThrowIfCancellationRequested();
                while (true)
                {
                    var message = await _messageSerializer.Deserialize(_transport);
                    _logger.LogDebug($"Received message {message.message} from client: {message.sessionKey}.");
                    if (message.message is TcpEndOfFileMessage) break;
                    DispatchIncomming(message.sessionKey, message.identifier, message.message);
                }
            });
        }

        private async void DispatchIncomming(uint sessionKey, uint identifier, object message)
        {
            try
            {
                await ProcessIncommingMessage(sessionKey, identifier, (dynamic)message);
            }
            catch
            {

            }
        }

        private async Task ProcessIncommingMessage(uint sessionKey, uint identifier, TcpContentMessage message)
        {
            var client = await GetTcpClient(message.Host, message.Port, sessionKey);
            await client.GetStream().WriteAsync(message.Content, 0, message.Content.Length);
        }

        private readonly SemaphoreSlim _semSlim = new SemaphoreSlim(1);
        private async Task SendResponse(uint session, object message)
        {
            var id = _identifier++;
            await _responseDispatcher.Queue(async () =>
            {
                await _semSlim.WaitAsync();
                try
                {
                    _logger.LogDebug($"Sending message {message} to client: {session}.");
                    await _messageSerializer.Serialize(session, id, message, _transport);
                }
                finally
                {
                    _semSlim.Release();
                }
            });
        }

        private async Task<TcpClient> GetTcpClient(string host, int port, uint desiredSession)
        {
            var key = new TcpClientKey { Host = host, Port = port, Session = desiredSession };
            var client = _tcpClients.GetOrAdd(key, k => new TcpClient());
            if (!client.Connected)
            {
                await client.ConnectAsync(host, port);
                ReceiveRemoteResponse(desiredSession, host, port, client.GetStream());
            }
            return client;
        }

        private async void ReceiveRemoteResponse(uint session, string host, int port, Stream stream)
        {
            while (true)
            {
                try
                {
                    var buffer = new byte[4096];
                    var read = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (read == 0)
                        await SendResponse(session, new TcpEndOfFileMessage());
                    else
                    {
                        var dst = new byte[read];
                        Array.Copy(buffer, dst, read);
                        await SendResponse(session, new TcpContentMessage
                        {
                            Host = host,
                            Port = (ushort)port,
                            Content = dst
                        });
                    }
                }
                catch (Exception)
                {
                    break;
                }
            }
        }

        private async Task ProcessIncommingMessage(uint sessionKey, uint identifier, object message)
        {

        }

        struct TcpClientKey : IEquatable<TcpClientKey>
        {
            public string Host;
            public int Port;
            public uint? Session;

            public bool Equals(TcpClientKey other)
            {
                return Host == other.Host && Port == other.Port && Session == other.Session;
            }

            public override bool Equals(object obj)
            {
                if (obj is TcpClientKey other)
                    return Equals(other);
                return false;
            }

            public override int GetHashCode()
            {
                return (Host?.GetHashCode() ?? 0) ^ Port.GetHashCode() ^ (Session?.GetHashCode() ?? 0);
            }
        }
    }
}
