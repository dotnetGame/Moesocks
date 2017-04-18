using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moesocks.Client.Services.Security;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Moesocks.Client.Services.Network
{
    class ConnectionRouter : IConnectionRouter, IMessageBus
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private readonly SecuritySettings _secSettings;
        private readonly SecureTransportSession _secureTransport;
        private readonly HttpProxyProvider _httpProxySession;
        private CancellationTokenSource _cts;
        private readonly ConnectionRouterSettings _settings;

        public ConnectionRouter(IOptions<ConnectionRouterSettings> settings, IOptions<SecuritySettings> securitySettings, ILoggerFactory loggerFactory)
        {
            _settings = settings.Value;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<ConnectionRouter>();
            _secSettings = securitySettings.Value;
            _secureTransport = new SecureTransportSession(new SecureTransportSessionSettings
            {
                Certificate = new X509Certificate2(securitySettings.Value.ServerCertificateFileName, securitySettings.Value.ServerCertificatePassword),
                MaxRandomBytesLength = securitySettings.Value.MaxRandomBytesLength,
                ServerEndPoint = new DnsEndPoint(settings.Value.ServerAddress, settings.Value.ServerPort)
            }, _loggerFactory);
            _httpProxySession = new HttpProxyProvider(settings.Value.Http, this, _loggerFactory);
        }

        public Task<(uint identifier, object message)> ReceiveAsync(uint sessionKey)
        {
            throw new NotImplementedException();
        }

        public Task SendAsync(uint sessionKey, uint identifier, object message)
        {
            throw new NotImplementedException();
        }

        public async void Startup()
        {
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            try
            {
                IEProxyHelper.SetProxy($@"http={_settings.Http.LocalIPAddress}:{_settings.Http.LocalPort},https={_settings.Http.LocalIPAddress}:{_settings.Http.LocalPort}");
                var tasks = new[] { _httpProxySession.Startup(token) };
                await Task.WhenAll(tasks);
            }
            finally
            {
                IEProxyHelper.UnsetProxy();
            }
        }

        public void Stop()
        {
            if (_cts != null)
                _cts.Cancel();
            IEProxyHelper.UnsetProxy();
        }
    }
}
