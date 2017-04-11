using Microsoft.Extensions.Logging;
using Moesocks.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Tomato.Threading;

namespace Moesocks.Client.Services.Security
{
    public class SecureTransportSessionSettings
    {
        public X509Certificate2 Certificate { get; set; }
        public DnsEndPoint ServerEndPoint { get; set; }
        public ushort MaxRandomBytesLength { get; set; }
    }

    public class SecureTransportSession : SecureTransportSessionBase
    {
        private readonly SecureTransportSessionSettings _settings;

        public SecureTransportSession(SecureTransportSessionSettings settings, ILoggerFactory loggerFactory)
            :base(new TcpClient(), settings.MaxRandomBytesLength, loggerFactory)
        {
            _settings = settings;
        }

        protected override async Task<Stream> AuthenticateAsync()
        {
            await Client.ConnectAsync(_settings.ServerEndPoint.Host, _settings.ServerEndPoint.Port);
            var netStream = new SslStream(Client.GetStream(), true, OnRemoteCertificateValidation, OnLocalCertificationValidation);
            await netStream.AuthenticateAsServerAsync(_settings.Certificate, true, SslProtocols.Tls12, false);
            return netStream;
        }

        private X509Certificate OnLocalCertificationValidation(object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers)
        {
            return _settings.Certificate;
        }

        private bool OnRemoteCertificateValidation(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return chain.Build((X509Certificate2)certificate);
        }
    }
}
