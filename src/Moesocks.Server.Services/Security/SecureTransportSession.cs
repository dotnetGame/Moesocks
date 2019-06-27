using Microsoft.Extensions.Logging;
using Moesocks.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Moesocks.Server.Services.Security
{
    public class SecureTransportSessionSettings
    {
        public X509Certificate2 Certificate { get; set; }

        public ushort MaxRandomBytesLength { get; set; }
    }

    public class SecureTransportSession : SecureTransportSessionBase
    {
        private readonly SecureTransportSessionSettings _settings;
        private readonly TcpClient _tcpClient;

        public SecureTransportSession(TcpClient tcpClient, SecureTransportSessionSettings settings, ILoggerFactory loggerFactory)
            :base(settings.MaxRandomBytesLength, loggerFactory)
        {
            _settings = settings;
            _tcpClient = tcpClient;
        }

        protected override async Task<Stream> AuthenticateAsync()
        {
            var netStream = new SslStream(_tcpClient.GetStream(), false, OnRemoteCertificateValidation, OnLocalCertificationValidation);
            await netStream.AuthenticateAsClientAsync(string.Empty, new X509CertificateCollection
            {
                _settings.Certificate
            }, SslProtocols.Tls12, false);
            return netStream;
        }

        private X509Certificate OnLocalCertificationValidation(object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers)
        {
            return _settings.Certificate;
        }

        private bool OnRemoteCertificateValidation(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return certificate.Issuer == _settings.Certificate.Subject;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
                _tcpClient.Dispose();
        }
    }
}
