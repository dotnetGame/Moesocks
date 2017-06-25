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
using System.Threading.Tasks.Dataflow;

namespace Moesocks.Server.Services.Network
{
    class ProxySession
    {
        private readonly SecureTransportSession _transport;
        private readonly ILogger _logger;
        private readonly MessageSerializer _messageSerializer;
        private readonly ConcurrentDictionary<TcpClientKey, TcpClientEntry> _tcpClients = new ConcurrentDictionary<TcpClientKey, TcpClientEntry>();
        private readonly ActionBlock<(uint session, object message)> _responseDispatcher;
        private uint _identifier;

        public ProxySession(SecureTransportSession transport, ILoggerFactory loggerFactory)
        {
            _transport = transport;
            _logger = loggerFactory.CreateLogger<ProxySession>();
            _messageSerializer = new MessageSerializer();
            _responseDispatcher = new ActionBlock<(uint session, object message)>(DispatchResponse);
            _responseDispatcher.Completion.ContinueWith(OnDispatcherCompleted);
        }

        public Task Run(CancellationToken token)
        {
            return Task.Run(async () =>
            {
                try
                {
                    token.ThrowIfCancellationRequested();
                    while (!_responseDispatcher.Completion.IsCompleted)
                    {
                        var message = await _messageSerializer.Deserialize(_transport);
                        _logger.LogDebug($"Received message {message.message} from client: {message.sessionKey}.");
                        DispatchIncomming(message.sessionKey, message.identifier, message.message);
                    }
                }
                catch
                {
                    _responseDispatcher.Complete();
                    throw;
                }
            });
        }

        private void OnDispatcherCompleted(Task arg1)
        {
            _transport.Dispose();
        }

        private async void DispatchIncomming(uint sessionKey, uint identifier, object message)
        {
            try
            {
                await ProcessIncommingMessage(sessionKey, identifier, (dynamic)message);
            }
            catch
            {
                _responseDispatcher.Complete();
            }
        }

        private async Task ProcessIncommingMessage(uint sessionKey, uint identifier, TcpContentMessage message)
        {
            var client = await GetTcpClient(message.Host, message.Port, sessionKey);
            await client.Client.GetStream().WriteAsync(message.Content, 0, message.Content.Length);
        }

        private async Task ProcessIncommingMessage(uint sessionKey, uint identifier, TcpEndOfFileMessage message)
        {
            var client = await GetTcpClient(message.Host, message.Port, sessionKey);
            client.Client.Client.Shutdown(SocketShutdown.Send);
            ReleaseTcpClient(sessionKey, message.Host, message.Port, client);
        }

        private async Task SendResponse(uint session, object message)
        {
            if (!_responseDispatcher.Completion.IsCompleted)
                await _responseDispatcher.SendAsync((session, message));
        }

        private async Task DispatchResponse((uint session, object message) response)
        {
            try
            {
                var id = _identifier++;
                await _messageSerializer.Serialize(response.session, id, response.message, _transport);
                _logger.LogDebug($"Sent message {response.message} to client: {response.session}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(new EventId(), ex.Message, ex);
                throw;
            }
        }

        private async Task<TcpClientEntry> GetTcpClient(string host, int port, uint desiredSession)
        {
            var key = new TcpClientKey { Host = host, Port = port, Session = desiredSession };
            var client = _tcpClients.GetOrAdd(key, k => new TcpClientEntry { Client = new TcpClient() });
            if (!client.Client.Connected)
            {
                try
                {
                    await client.Client.ConnectAsync(host, port);
                }
                catch
                {
                    client.Client.Dispose();
                    client.Client = new TcpClient();
                    await client.Client.ConnectAsync(host, port);
                }
                client.CloseCount = 0;
                ReceiveRemoteResponse(desiredSession, host, port, client);
            }
            return client;
        }

        private void RemoveTcpClient(string host, int port, uint session)
        {
            var key = new TcpClientKey { Host = host, Port = port, Session = session };
            if (_tcpClients.TryRemove(key, out var client))
            {
                if (client.Client.Connected)
                    client.Client.GetStream().Dispose();
                client.Client.Dispose();
            }
        }

        class TcpClientEntry
        {
            public TcpClient Client { get; set; }
            public int CloseCount = 0;
        }

        private void ReleaseTcpClient(uint session, string host, int port, TcpClientEntry client)
        {
            if (Interlocked.Increment(ref client.CloseCount) == 2)
                RemoveTcpClient(host, port, session);
        }

        private async void ReceiveRemoteResponse(uint session, string host, int port, TcpClientEntry client)
        {
            while (true)
            {
                try
                {
                    var stream = client.Client.GetStream();
                    var buffer = new byte[4096];
                    var read = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (read == 0)
                    {
                        ReleaseTcpClient(session, host, port, client);
                        await SendResponse(session, new TcpEndOfFileMessage
                        {
                            Host = host,
                            Port = (ushort)port
                        });
                        break;
                    }
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
                    RemoveTcpClient(host, port, session);
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
