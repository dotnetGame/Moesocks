using Moesocks.Socks5.Protocol.Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;

namespace Moesocks.Socks5
{
    public abstract class Socks5ProxySessionBase : IDisposable
    {
        protected Stream RemoteStream { get; }

        public Socks5ProxySessionBase(Stream remoteStream)
        {
            RemoteStream = remoteStream;
        }

        public async Task Run(CancellationToken token)
        {
            using (var br = new BinaryReader(RemoteStream, Encoding.UTF8, true))
            using (var bw = new BinaryWriter(RemoteStream, Encoding.UTF8, true))
            {
                await HandshakeAsync(br, bw);
                await SocksAsync(br, bw);
            }
            await RunTunnel();
        }

        protected abstract Task RunTunnel();

        private async Task HandshakeAsync(BinaryReader reader, BinaryWriter writer)
        {
            var message = await HandshakeRequestMessage.ReadFromAsync(reader);
            await new HandshakeResponseMessage
            {
                VER = HandshakeRequestMessage.Socks5Ver,
                METHOD = FindAcceptableAuthenticateMethod(message.METHODS)
            }.WriteToAsync(writer);
        }

        private AuthenticateMethod FindAcceptableAuthenticateMethod(byte[] methods)
        {
            if (Array.IndexOf(methods, (byte)AuthenticateMethod.None) != -1)
                return AuthenticateMethod.None;
            return AuthenticateMethod.NotSupported;
        }

        private async Task SocksAsync(BinaryReader reader, BinaryWriter writer)
        {
            var message = await SocksRequestMessage.ReadFromAsync(reader);
            switch (message.CMD)
            {
                case CommandType.Connect:
                    await ConnectAsync(writer, message.ATYP, message.DEST);
                    break;
                case CommandType.Bind:
                    await BindAsync(writer, message.ATYP, message.DEST);
                    break;
                case CommandType.Udp:
                    throw new NotSupportedException("Udp is not supported.");
                default:
                    throw new InvalidOperationException("Invalid command type.");
            }
        }

        private Task BindAsync(BinaryWriter writer, AddressType aTYP, DnsEndPoint dEST)
        {
            throw new NotSupportedException("Bind command is not supported.");
        }

        private async Task ConnectAsync(BinaryWriter writer, AddressType addressType, DnsEndPoint dest)
        {
            var response = new SocksResponseMessage
            {
                VER = HandshakeRequestMessage.Socks5Ver,
                ATYP = addressType,
                DEST = dest
            };
            try
            {
                await ConnectAsync(addressType, dest);
                response.REP = SocksResponseStatus.Succeeded;
            }
            catch (Exception)
            {
                response.REP = SocksResponseStatus.GenericFailure;
            }
            await response.WriteToAsync(writer);
            if (response.REP != SocksResponseStatus.Succeeded)
                RemoteStream.Dispose();
        }

        protected abstract Task ConnectAsync(AddressType addressType, DnsEndPoint dest);

        #region IDisposable Support
        private bool disposedValue = false; // 要检测冗余调用

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    RemoteStream.Dispose();
                }
                disposedValue = true;
            }
        }

        // 添加此代码以正确实现可处置模式。
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
