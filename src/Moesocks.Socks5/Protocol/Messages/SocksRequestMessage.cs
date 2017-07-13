using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Moesocks.Socks5.Protocol.Messages
{
    public enum AddressType : byte
    {
        IPv4 = 1,
        DomainName = 3,
        IPv6 = 4
    }

    public enum CommandType
    {
        Connect = 1,
        Bind = 2,
        Udp = 3
    }

    public sealed class SocksRequestMessage
    {
        public byte VER;
        public CommandType CMD;
        public byte RECV;
        public AddressType ATYP;
        public DnsEndPoint DEST;


        public static Task<SocksRequestMessage> ReadFromAsync(BinaryReader reader)
        {
            var ver = reader.ReadByte();
            var cmd = (CommandType)reader.ReadByte();
            var recv = reader.ReadByte();
            var atyp = (AddressType)reader.ReadByte();
            var destAddr = ReadDestAddress(reader, atyp);
            var port = reader.ReadUInt16().FromBigEndian();
            return Task.FromResult(new SocksRequestMessage
            {
                VER = ver,
                CMD = cmd,
                RECV = recv,
                ATYP = atyp,
                DEST = new DnsEndPoint(destAddr, port)
            }.Validate());
        }

        private static string ReadDestAddress(BinaryReader reader, AddressType type)
        {
            switch (type)
            {
                case AddressType.IPv4:
                    return new IPAddress(reader.ReadBytes(4)).ToString();
                case AddressType.DomainName:
                    {
                        var len = reader.ReadByte();
                        return Encoding.UTF8.GetString(reader.ReadBytes(len));
                    }
                case AddressType.IPv6:
                    return new IPAddress(reader.ReadBytes(16)).ToString();
                default:
                    throw new NotSupportedException("Not supported address type.");
            }
        }

        public SocksRequestMessage Validate()
        {
            if (VER != HandshakeRequestMessage.Socks5Ver)
                throw new InvalidDataException("Invalid protocol version.");
            if (!Enum.IsDefined(typeof(CommandType), CMD))
                throw new InvalidDataException("Invalid command type.");
            if (RECV != 0)
                throw new InvalidDataException("recv must be zero.");
            if (!Enum.IsDefined(typeof(AddressType), ATYP))
                throw new InvalidDataException("Invalid address type.");
            return this;
        }
    }

    static class EndianExtensions
    {
        public static ushort FromBigEndian(this ushort value)
        {
            if (BitConverter.IsLittleEndian)
                return (ushort)((byte)(value >> 8) | ((byte)(value) << 8));
            return value;
        }
    }
}
