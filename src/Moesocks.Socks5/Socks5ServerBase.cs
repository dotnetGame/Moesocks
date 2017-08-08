using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Moesocks.Socks5
{
    public abstract class Socks5ServerBase
    {
        private readonly TcpListener _listener;
        protected readonly ILoggerFactory _loggerFactory;
        protected readonly ILogger _logger;

        public Socks5ServerBase(IPEndPoint listenerEP, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<Socks5ServerBase>();
            _listener = new TcpListener(listenerEP);
        }

        public async Task Startup(CancellationToken token)
        {
            _listener.Start();
            try
            {
                while (!token.IsCancellationRequested)
                {
                    DispatchIncoming(await _listener.AcceptTcpClientAsync(), token);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(0, ex, ex.Message);
            }
            finally
            {
                _listener.Stop();
            }
        }

        private async void DispatchIncoming(TcpClient tcpClient, CancellationToken token)
        {
            try
            {
                using (tcpClient)
                using (var session = CreateSession(tcpClient.GetStream(), tcpClient.Client))
                {
                    await session.Run(token);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(0, ex, ex.Message);
            }
        }

        protected abstract Socks5ProxySessionBase CreateSession(Stream remoteStream, Socket socket);
    }
}
