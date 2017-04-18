using Moesocks.Protocol;
using Moesocks.Protocol.Messages;
using Moesocks.Server.Services.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tomato.Threading;
using TcpContentMessage = Moesocks.Protocol.Messages.TcpContentMessage;
using TcpEndOfFileMessage = Moesocks.Protocol.Messages.TcpEndOfFileMessage;

namespace Moesocks.Server.Services.Network
{
    class ProxySession
    {
        private readonly SecureTransportSession _transport;
        private readonly MessageSerializer _messageSerializer;
        private readonly HttpClient _httpClient;
        private readonly OperationQueue _responseDispatcher = new OperationQueue(1);
        public ProxySession(SecureTransportSession transport)
        {
            _transport = transport;
            _messageSerializer = new MessageSerializer();
            _httpClient = new HttpClient();
        }

        public Task Run(CancellationToken token)
        {
            return Task.Run(async () =>
            {
                token.ThrowIfCancellationRequested();
                while (true)
                {
                    var message = await _messageSerializer.Deserialize(_transport);
                    DispatchIncomming(message.id, message.mesage);
                }
            });
        }

        private async void DispatchIncomming(uint id, object message)
        {
            try
            {
                await ProcessIncommingMessage(id, (dynamic)message);
            }
            catch
            {

            }
        }

        private async Task ProcessIncommingMessage(uint id, TcpContentMessage message)
        {
            var request = new System.Net.Http.HttpRequestMessage(new HttpMethod(message.Method), message.Uri);
            if (message.Body != null)
                request.Content = new StreamContent(new MemoryStream(message.Body));
            foreach (var header in message.Headers)
            {
                try
                {
                    request.Headers.Add(header.Key, header.Value);
                }
                catch(InvalidOperationException)
                {
                    if (request.Content != null)
                        request.Content.Headers.Add(header.Key, header.Value);
                }
            }
            var response = await _httpClient.SendAsync(request);
            var responseMessage = new TcpEndOfFileMessage
            {
                StatusCode = (uint)response.StatusCode,
                Headers = new Dictionary<string, string>()
            };
            foreach (var header in response.Headers)
                responseMessage.Headers.Add(header.Key, string.Join(";", header.Value));
            if (response.Content != null)
            {
                responseMessage.Body = await response.Content.ReadAsByteArrayAsync();
                foreach (var header in response.Content.Headers)
                    responseMessage.Headers.Add(header.Key, string.Join(";", header.Value));
            }
            await SendResponse(id, responseMessage);
        }

        private async Task SendResponse(uint id, object message)
        {
            await _responseDispatcher.Queue(async () =>
            {
                await _messageSerializer.Serialize(id, message, _transport);
            });
        }

        private async Task ProcessIncommingMessage(uint id, object message)
        {

        }
    }
}
