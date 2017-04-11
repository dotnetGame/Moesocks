using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Net.Sockets;
using Microsoft.Extensions.Options;
using System.Net;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading;
using Moesocks.Client.Services.Security;
using System.Security.Cryptography.X509Certificates;
using Moesocks.Protocol.Messages;
using Moesocks.Protocol;
using Tomato.Threading;
using System.Linq;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http.Features;

namespace Moesocks.Client.Services.Network
{
    class WebProxyProvider
    {
        private readonly DnsEndPoint _serverEndPoint;
        private readonly ILogger _logger;
        private readonly SecureTransportSession _transport;
        private readonly MessageSerializer _messageSerializer;
        private readonly OperationQueue _requestDispatcher = new OperationQueue(1);
        private readonly ConcurrentDictionary<uint, TaskCompletionSource<object>> _responseWaiters = new ConcurrentDictionary<uint, TaskCompletionSource<object>>();
        private uint _packetId;

        public WebProxyProvider(IOptions<WebProxySettings> settings, ILoggerFactory loggerFactory)
        {
            _serverEndPoint = new DnsEndPoint(settings.Value.ServerAddress, settings.Value.ServerPort);
            _logger = loggerFactory.CreateLogger(typeof(WebProxyProvider));
            _messageSerializer = new MessageSerializer();
            _transport = new SecureTransportSession(new SecureTransportSessionSettings
            {
                ServerEndPoint = _serverEndPoint,
                MaxRandomBytesLength = settings.Value.MaxRandomBytesLength,
                Certificate = new X509Certificate2(settings.Value.ClientCertificateFileName, settings.Value.ClientCertificatePassword)
            }, loggerFactory);
            Run(default(CancellationToken));
        }

        public async Task ProcessRequest(HttpContext context)
        {
            var request = context.Request;
            var reqMessage = new HttpRequestMessage
            {
                Uri = context.Features.Get<IHttpRequestFeature>().RawTarget,
                Method = request.Method,
                Headers = request.Headers?.ToDictionary(o => o.Key, o => o.Value.ToString())
            };
            if ((request.ContentLength ?? 0) != 0)
            {
                var body = new MemoryStream();
                await request.Body.CopyToAsync(body);
                reqMessage.Body = body.ToArray();
            }
            var response = await SendMessage(_packetId++, reqMessage);
            context.Response.StatusCode = (int)response.StatusCode;
            if (response.Headers != null)
            {
                foreach (var header in response.Headers)
                    context.Response.Headers.Add(header.Key, header.Value);
            }
            if (response.Body != null && response.Body.Length != 0)
                await context.Response.Body.WriteAsync(response.Body, 0, response.Body.Length);
        }

        private async Task<HttpResponseMessage> SendMessage(uint packetId, HttpRequestMessage message)
        {
            var waiter = AddPacketWaiter(packetId);
            try
            {
                await _requestDispatcher.Queue(async () =>
                {
                    await _messageSerializer.Serialize(packetId, message, _transport);
                });
                await Task.WhenAny(waiter, ThrowTimeout());
                return (HttpResponseMessage)waiter.Result;
            }
            finally
            {
                RemovePacketWaiter(packetId);
            }
        }


        public Task Run(CancellationToken token)
        {
            return Task.Run(async () =>
            {
                while (true)
                {
                    token.ThrowIfCancellationRequested();
                    var message = await _messageSerializer.Deserialize(_transport);
                    DispatchIncomming(message.id, message.mesage);
                }
            });
        }

        private void DispatchIncomming(uint packetId, object message)
        {
            try
            {
                TaskCompletionSource<object> waiter;
                if (_responseWaiters.TryRemove(packetId, out waiter))
                    waiter.SetResult(message);
            }
            catch
            {

            }
        }

        private void RemovePacketWaiter(uint packetId)
        {
            TaskCompletionSource<object> waiter;
            _responseWaiters.TryRemove(packetId, out waiter);
        }

        private async Task ThrowTimeout()
        {
            await Task.Delay(TimeSpan.FromSeconds(10));
            throw new TimeoutException();
        }

        private Task<object> AddPacketWaiter(uint packetId)
        {
            var completionSource = new TaskCompletionSource<object>();
            if (!_responseWaiters.TryAdd(packetId, completionSource))
                throw new ArgumentException(nameof(packetId));
            return completionSource.Task;
        }
    }
}
