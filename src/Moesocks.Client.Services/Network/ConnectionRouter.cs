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
using Tomato.Threading;

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
        private readonly MessageSerializer _serializer = new MessageSerializer();
        private readonly OperationQueue _writeQueue = new OperationQueue(1);

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

        private readonly ConcurrentDictionary<uint, Action<uint, object>> _receivers = new ConcurrentDictionary<uint, Action<uint, object>>();

        public void BeginReceive(uint sessionKey, Action<uint, object> receiver)
        {
            _receivers[sessionKey] = receiver;
        }

        public void EndReceive(uint sessionKey)
        {
            _receivers.TryRemove(sessionKey, out var receiver);
        }

        private readonly SemaphoreSlim _semSlim = new SemaphoreSlim(1);

        public Task SendAsync(uint sessionKey, uint identifier, object message)
        {
            return _writeQueue.Queue(async() =>
            {
                await _semSlim.WaitAsync();
                try
                {
                    _logger.LogDebug($"client: {sessionKey} Sending message {message} to server.");
                    await _serializer.Serialize(sessionKey, identifier, message, _secureTransport);
                }
                finally
                {
                    _semSlim.Release();
                }
            });
        }

        public async void Startup()
        {
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            try
            {
                //http={_settings.Http.LocalIPAddress}:{_settings.Http.LocalPort},
                IEProxyHelper.SetProxy($@"https={_settings.Http.LocalIPAddress}:{_settings.Http.LocalPort}");
                var tasks = new[] { _httpProxySession.Startup(token), BeginReceiveMessages(token) };
                await Task.WhenAll(tasks);
            }
            finally
            {
                IEProxyHelper.UnsetProxy();
            }
        }

        private async Task BeginReceiveMessages(CancellationToken cancellationToken)
        {
            while(true)
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
                catch(OperationCanceledException)
                {
                    _receivers.Clear();
                    break;
                }
                catch (Exception)
                {
                    _receivers.Clear();
                }
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
