using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moesocks.Server.Services.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Moesocks.Server.Services.Network
{
    class ConnectionRouter : IConnectionRouter
    {
        private readonly TcpListener _listener;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private readonly SecuritySettings _secSettings;
        private readonly X509Certificate2 _serverCertificate;
        private int _eventId;
        private CancellationTokenSource _cts;

        public ConnectionRouter(IOptions<ConnectionRouterSettings> settings, IOptions<SecuritySettings> securitySettings, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<ConnectionRouter>();
            _listener = CreateListener(settings.Value);
            _secSettings = securitySettings.Value;
            _serverCertificate = new X509Certificate2(securitySettings.Value.ServerCertificateFileName);
        }

        private TcpListener CreateListener(ConnectionRouterSettings settings)
        {
            var listener = new TcpListener(IPAddress.Parse(settings.ServerIPAddress), settings.ServerPort);
            return listener;
        }

        public async void Startup()
        {
            _listener.Start();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            await Task.Run(async () =>
            {
                _logger.LogInformation($"Start listening at {(IPEndPoint)_listener.LocalEndpoint}...");
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
            }, _cts.Token);
        }

        private async void DispatchIncoming(TcpClient tcpClient)
        {
            try
            {
                var token = _cts.Token;
                using (tcpClient)
                {
                    _logger.LogInformation($"Accept client from: {(IPEndPoint)tcpClient.Client.RemoteEndPoint}.");
                    using (var transport = new SecureTransportSession(tcpClient, new SecureTransportSessionSettings
                    {
                        Certificate = _serverCertificate,
                        MaxRandomBytesLength = _secSettings.MaxRandomBytesLength
                    }, _loggerFactory))
                    {
                        await transport.ConnectAsync();
                        var session = new ProxySession(transport, _loggerFactory);
                        await session.Run(token);
                    }
                }
                _logger.LogInformation($"Closed client from: {(IPEndPoint)tcpClient.Client.RemoteEndPoint}.");
            }
            catch(ObjectDisposedException)
            {

            }
            catch (Exception ex)
            {
                _logger.LogError(Interlocked.Increment(ref _eventId), ex, ex.Message);
            }
        }

        public void Stop()
        {
            if (_cts != null)
                _cts.Cancel();
        }
    }
}
