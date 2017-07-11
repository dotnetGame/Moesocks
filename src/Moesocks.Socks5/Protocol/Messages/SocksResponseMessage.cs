using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Moesocks.Socks5.Protocol.Messages
{
    public enum SocksResponseStatus : byte
    {
        Succeeded = 0,
        GenericFailure = 1
    }

    public sealed class SocksResponseMessage
    {
        public byte VER;
        public SocksResponseStatus REP;
        public byte RSV;
        public AddressType ATYP;
        public DnsEndPoint DEST;

        public Task WriteToAsync(BinaryWriter writer)
        {
            writer.Write(VER);
            writer.Write((byte)REP);
            writer.Write(RSV);
            writer.Write((byte)ATYP);
            WriteDestAddress(writer);
            writer.Write(((ushort)DEST.Port).FromBigEndian());
            return Task.CompletedTask;
        }

        private void WriteDestAddress(BinaryWriter writer)
        {
            switch (ATYP)
            {
                case AddressType.IPv4:
                    writer.Write(IPAddress.Parse(DEST.Host).GetAddressBytes());
                    break;
                case AddressType.DomainName:
                    writer.Write((byte)DEST.Host.Length);
                    writer.Write(Encoding.UTF8.GetBytes(DEST.Host));
                    break;
                case AddressType.IPv6:
                    writer.Write(IPAddress.Parse(DEST.Host).GetAddressBytes());
                    break;
                default:
                    throw new NotSupportedException("Not supported address type.");
            }
        }
    }
}
