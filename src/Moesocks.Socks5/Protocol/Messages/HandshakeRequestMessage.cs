using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Moesocks.Socks5.Protocol.Messages
{
    public enum AuthenticateMethod : byte
    {
        None = 0,
        GSSAPI = 1,
        UserNamePassword = 2,
        IANA = 3,
        NotSupported = 0xFF
    }

    public sealed class HandshakeRequestMessage
    {
        public const byte Socks5Ver = 0x5;

        public byte VER;
        public byte NMETHODS;
        public byte[] METHODS;

        public static Task<HandshakeRequestMessage> ReadFromAsync(BinaryReader reader)
        {
            var ver = reader.ReadByte();
            var nMethods = reader.ReadByte();
            var methods = reader.ReadBytes(nMethods);
            return Task.FromResult(new HandshakeRequestMessage
            {
                VER = ver,
                NMETHODS = nMethods,
                METHODS = methods
            }.Validate());
        }

        public HandshakeRequestMessage Validate()
        {
            if (VER != Socks5Ver)
                throw new InvalidDataException("Invalid protocol version.");
            if (NMETHODS != METHODS.Length)
                throw new InvalidDataException("Invalid methods count.");
            return this;
        }
    }
}
