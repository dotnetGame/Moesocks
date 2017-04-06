using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Moesocks.Client.Services.Security
{
    public enum SecureTransportSessionState
    {
        Disconnected,
        Connected,
        Error
    }

    public class SecureTransportSessionSettings
    {
        public X509Certificate2 Certificate { get; set; }
        public DnsEndPoint ServerEndPoint { get; set; }
    }

    public class SecureTransportSession
    {
        public SecureTransportSessionState State { get; } = SecureTransportSessionState.Disconnected;
        private readonly TcpClient _tcpClient;
        private readonly SecureTransportSessionSettings _settings;

        public SecureTransportSession(SecureTransportSessionSettings settings)
        {
            _settings = settings;
            _tcpClient = new TcpClient();
        }

        public async void Start()
        {
            if (State == SecureTransportSessionState.Connected)
                throw new InvalidOperationException("Session is already started.");
            await _tcpClient.ConnectAsync(_settings.ServerEndPoint.Host, _settings.ServerEndPoint.Port);
            BeginSecurityHandshake();
        }

        private void BeginSecurityHandshake()
        {
            var stream = _tcpClient.GetStream();
            //stream.WriteAsync()
        }
    }
}
