using Moesocks.Protocol;
using Moesocks.Protocol.Messages;
using Moesocks.Server.Services.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HttpRequestMessage = Moesocks.Protocol.Messages.HttpRequestMessage;
using HttpResponseMessage = Moesocks.Protocol.Messages.HttpResponseMessage;

namespace Moesocks.Server.Services.Network
{
    class ProxySession
    {
        private readonly SecureTransportSession _transport;
        private readonly MessageSerializer _messageSerializer;
        private readonly HttpClient _httpClient;
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
                while (true)
                {
                    token.ThrowIfCancellationRequested();
                    var message = await _messageSerializer.Deserialize(_transport);
                    DispatchIncomming(message);
                }
            });
        }

        private void DispatchIncomming(object message)
        {
            try
            {
                (this as dynamic).ProcessIncommingMessage(message);
            }
            catch
            {

            }
        }

        private async void ProcessIncommingMessage(HttpRequestMessage message)
        {
            var request = new System.Net.Http.HttpRequestMessage(new HttpMethod(message.Method), message.Uri);
            foreach (var header in message.Headers)
                request.Headers.Add(header.Key, header.Value);
            request.Content = new StreamContent(new MemoryStream(message.Body));
            var response = await _httpClient.SendAsync(request);
            //var responseMessage = new HttpResponseMessage
            //{
            //    StatusCode = (uint)response.StatusCode,
            //    Headers = response.Headers.
            //}
        }

        private void ProcessIncommingMessage(object message)
        {

        }
    }
}
