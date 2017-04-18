using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Moesocks.Client
{
    public class HttpProxySettings
    {
        public string LocalIPAddress { get; set; }
        public int LocalPort { get; set; }
    }

    public class ConnectionRouterSettings : IOptions<ConnectionRouterSettings>
    {
        ConnectionRouterSettings IOptions<ConnectionRouterSettings>.Value => this;

        public HttpProxySettings Http { get; set; }

        public string ServerAddress { get; set; }
        public int ServerPort { get; set; }
    }

    public class SecuritySettings : IOptions<SecuritySettings>
    {
        SecuritySettings IOptions<SecuritySettings>.Value => this;

        public string ServerCertificateFileName { get; set; }
        public string ServerCertificatePassword { get; set; }
        public ushort MaxRandomBytesLength { get; set; }
    }

    public interface IConnectionRouter
    {
        void Startup();
        void Stop();
    }
}
