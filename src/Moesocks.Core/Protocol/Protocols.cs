using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Moesocks.Protocol
{
    public static class Protocols
    {
        public const ushort ProtocolVersion = 1;

        public enum MessageType : ushort
        {
            TcpContent = 1,
            TcpEndOfFile = 2,
            TcpError = 3
        }
    }

    public struct PacketHeader
    {
        public ushort ProtocolVersion;
        public Protocols.MessageType MessageType;
        public ushort MessageVersion;
        public uint SessionKey;
        public uint Identifier;

        public void VerifyAndSetProtocolVersion(ushort value)
        {
            if (value != Protocols.ProtocolVersion)
                throw new InvalidDataException($"Invalid protocol version: {value}, expected: {Protocols.ProtocolVersion}.");
            ProtocolVersion = value;
        }

        public void VerifyAndSetMessageType(ushort value)
        {
            if(!Enum.IsDefined(typeof(Protocols.MessageType), value))
                throw new InvalidDataException($"Invalid message kind: {value}.");
            MessageType = (Protocols.MessageType)value;
        }
    }
}
