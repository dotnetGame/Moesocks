using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moesocks.Client.Services.Security;
using Moesocks.Protocol;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Moesocks.Client.Services.Network
{
    class ConnectionRouter : IConnectionRouter, IMessageBus
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private readonly SecuritySettings _secSettings;
        private readonly SecureTransportSession _secureTransport;

        private readonly HttpProxyProvider _httpProxySession;
        private readonly Socks5ProxyProvider _socks5ProxySession;

        private CancellationTokenSource _cts;
        private readonly ConnectionRouterSettings _settings;
        private readonly MessageSerializer _serializer = new MessageSerializer();
        private readonly ActionBlock<(uint sessionKey, uint identifier, object message)> _requestDispather;
        private readonly IPlatformProvider _platformProvider;

        public ConnectionRouter(IOptions<ConnectionRouterSettings> settings, IOptions<SecuritySettings> securitySettings, IPlatformProvider platformProvider, ILoggerFactory loggerFactory)
        {
            _settings = settings.Value;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<ConnectionRouter>();
            _secSettings = securitySettings.Value;
            _platformProvider = platformProvider;
            _secureTransport = new SecureTransportSession(new SecureTransportSessionSettings
            {
                Certificate = new X509Certificate2(securitySettings.Value.ServerCertificateFileName, securitySettings.Value.ServerCertificatePassword),
                MaxRandomBytesLength = securitySettings.Value.MaxRandomBytesLength,
                ServerEndPoint = new DnsEndPoint(settings.Value.ServerAddress, settings.Value.ServerPort)
            }, _loggerFactory);
            _httpProxySession = new HttpProxyProvider(settings.Value.Http, this, _loggerFactory);
            _socks5ProxySession = new Socks5ProxyProvider(this, _loggerFactory);
            _requestDispather = new ActionBlock<(uint sessionKey, uint identifier, object message)>(DispatchRequest);
        }

        private readonly ConcurrentDictionary<uint, Action<uint, object>> _receivers = new ConcurrentDictionary<uint, Action<uint, object>>();

        public void BeginReceive(uint sessionKey, Action<uint, object> receiver)
        {
            _receivers[sessionKey] = receiver;
        }

        public void EndReceive(uint sessionKey)
        {
            _receivers.TryRemove(sessionKey, out var receiver);
        }

        public async Task SendAsync(uint sessionKey, uint identifier, object message)
        {
            if (!_requestDispather.Completion.IsCompleted)
                await _requestDispather.SendAsync((sessionKey, identifier, message));
        }

        private async Task DispatchRequest((uint sessionKey, uint identifier, object message) request)
        {
            try
            {
                _logger.LogDebug($"client: {request.sessionKey} Sending message {request.message} to server.");
                await _serializer.Serialize(request.sessionKey, request.identifier, request.message, _secureTransport);
            }
            catch (Exception ex)
            {
                _logger.LogError(default(EventId), ex.Message, ex);
                throw;
            }
        }

        public async void Startup()
        {
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            try
            {
                //http={_settings.Http.LocalIPAddress}:{_settings.Http.LocalPort},
                var tasks = new[]
                {
                    _httpProxySession.Startup(token),
                    _socks5ProxySession.Startup(token),
                    BeginReceiveMessages(token)
                };
                _platformProvider.SetProxy($@"https={_settings.Http.LocalIPAddress}:{_settings.Http.LocalPort}");
                await Task.WhenAll(tasks);
            }
            finally
            {
                _platformProvider.UnsetProxy();
            }
        }

        private async Task BeginReceiveMessages(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Proxy started.");
            while (true)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    (var sessionKey, var identifier, var message) = await _serializer.Deserialize(_secureTransport);
                    cancellationToken.ThrowIfCancellationRequested();
                    _logger.LogDebug($"client: {sessionKey} Received message {message} from server.");
                    if (_receivers.TryGetValue(sessionKey, out var receiver))
                        receiver(identifier, message);
                }
                catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
                {
                    _receivers.Clear();
                    _logger.LogWarning($"Proxy stopped.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(default(EventId), ex.Message, ex);
                    await Task.Delay(5000);
                }
            }
        }

        public void Stop()
        {
            if (_cts != null)
                _cts.Cancel();
            _platformProvider.UnsetProxy();
        }
    }
}
