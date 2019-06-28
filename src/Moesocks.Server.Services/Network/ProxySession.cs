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
        private readonly ConcurrentDictionary<uint, TcpClientEntry> _tcpClients = new ConcurrentDictionary<uint, TcpClientEntry>();
        private readonly ActionBlock<(uint session, object message)> _responseDispatcher;
        private uint _identifier;

        public ProxySession(SecureTransportSession transport, ILoggerFactory loggerFactory)
        {
            _transport = transport;
            _logger = loggerFactory.CreateLogger<ProxySession>();
            _messageSerializer = new MessageSerializer();
            _responseDispatcher = new ActionBlock<(uint session, object message)>(DispatchResponse, new ExecutionDataflowBlockOptions
            {
                EnsureOrdered = true,
                SingleProducerConstrained = true,
                MaxMessagesPerTask = 1,
                MaxDegreeOfParallelism = 1
            });
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
                    _transport.Close();
                    throw;
                }
            });
        }

        private void OnDispatcherCompleted(Task arg1)
        {
            _transport.Close();
        }

        private async void DispatchIncomming(uint sessionKey, uint identifier, object message)
        {
            try
            {
                switch (message)
                {
                    case TcpContentMessage m:
                        await ProcessIncommingMessage(sessionKey, identifier, m);
                        break;
                    case TcpEndOfFileMessage m:
                        await ProcessIncommingMessage(sessionKey, identifier, m);
                        break;
                    case TcpErrorMessage m:
                        await ProcessIncommingMessage(sessionKey, identifier, m);
                        break;
                }
            }
            catch
            {
                RemoveTcpClient(sessionKey);
                OnError(sessionKey);
            }
        }

        private async void OnError(uint sessionKey)
        {
            try
            {
                await SendResponse(sessionKey, new TcpErrorMessage());
            }
            catch
            {
            }
        }

        private async Task ProcessIncommingMessage(uint sessionKey, uint identifier, TcpContentMessage message)
        {
            var client = await GetTcpClient(sessionKey, message.Host, message.Port);
            await client.Stream.WriteAsync(message.Content, 0, message.Content.Length);
        }

        private async Task ProcessIncommingMessage(uint sessionKey, uint identifier, TcpEndOfFileMessage message)
        {
            var client = await GetTcpClient(sessionKey, message.Host, message.Port);
            client.Client.Client.Shutdown(SocketShutdown.Send);
            ReleaseTcpClient(sessionKey, client);
        }

        private Task ProcessIncommingMessage(uint sessionKey, uint identifier, TcpErrorMessage message)
        {
            RemoveTcpClient(sessionKey);
            return Task.CompletedTask;
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
                _logger.LogError(0, ex.Message, ex);
                _responseDispatcher.Complete();
                _transport.Close();
                throw;
            }
        }

        private async Task<TcpClientEntry> GetTcpClient(uint sessionKey, string host, int port)
        {
            var client = _tcpClients.GetOrAdd(sessionKey, k => new TcpClientEntry { Client = new TcpClient() });
            if (!client.Client.Connected)
            {
                await client.Client.ConnectAsync(host, port);
                client.Stream = client.Client.GetStream();
                client.CloseCount = 0;
                ReceiveRemoteResponse(sessionKey, host, port, client);
            }

            return client;
        }

        private void RemoveTcpClient(uint session)
        {
            if (_tcpClients.TryRemove(session, out var client))
            {
                if (client.Client.Connected)
                    client.Client.GetStream().Dispose();
                client.Client.Dispose();
            }
        }

        class TcpClientEntry
        {
            public TcpClient Client { get; set; }

            public Stream Stream { get; set; }

            public int CloseCount = 0;
        }

        private void ReleaseTcpClient(uint session, TcpClientEntry client)
        {
            if (Interlocked.Increment(ref client.CloseCount) == 2)
                RemoveTcpClient(session);
        }

        private async void ReceiveRemoteResponse(uint session, string host, int port, TcpClientEntry client)
        {
            var stream = client.Stream;
            var readBuffer = new byte[1024 * 16];

            while (true)
            {
                bool noNext = false;
                object message;
                try
                {
                    var read = await stream.ReadAsync(readBuffer, 0, readBuffer.Length);
                    if (read == 0)
                    {
                        message = new TcpEndOfFileMessage
                        {
                            Host = host,
                            Port = (ushort)port
                        };
                        noNext = true;
                    }
                    else
                    {
                        var dst = new byte[read];
                        Array.Copy(readBuffer, dst, read);
                        message = new TcpContentMessage
                        {
                            Host = host,
                            Port = (ushort)port,
                            Content = dst
                        };
                    }
                }
                catch (Exception)
                {
                    RemoveTcpClient(session);
                    OnError(session);
                    return;
                }

                await SendResponse(session, message);
                if (noNext)
                {
                    ReleaseTcpClient(session, client);
                    break;
                }
            }
        }
    }
}
