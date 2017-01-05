using Microsoft.Extensions.DependencyInjection;
using Moesocks.Security;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Moesocks.Server
{
    class ClientTier
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly TcpListener _tcp;
        private readonly UdpClient _udp;
        private readonly ConcurrentBag<TcpClientSession> _tcpSessions = new ConcurrentBag<TcpClientSession>();

        public ClientTier(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _tcp = new TcpListener(IPAddress.Any, 20000);
            _udp = new UdpClient(20000, AddressFamily.InterNetwork);
        }

        public async void Start()
        {
            _tcp.Start();
            var udpSession = ActivatorUtilities.CreateInstance<UdpClientSession>(_serviceProvider, _udp);
            udpSession.Start();

            while (true)
            {
                try
                {
                    Dispatch(await _tcp.AcceptTcpClientAsync());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        private void Dispatch(TcpClient client)
        {
            var session = ActivatorUtilities.CreateInstance<TcpClientSession>(_serviceProvider, client);
            _tcpSessions.Add(session);
            session.Reset();
        }
    }
}
