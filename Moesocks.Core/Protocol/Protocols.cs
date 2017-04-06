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

        public enum MessageIds : ushort
        {
            HttpRequest = 1
        }
    }

    public struct PacketHeader
    {
        public ushort ProtocolVersion;
        public Protocols.MessageIds MessageId;
        public ushort MessageVersion;

        public void VerifyAndSetProtocolVersion(ushort value)
        {
            if (value != Protocols.ProtocolVersion)
                throw new InvalidDataException($"Invalid protocol version: {value}, expected: {Protocols.ProtocolVersion}.");
            ProtocolVersion = value;
        }

        public void VerifyAndSetMessageId(ushort value)
        {
            if(!Enum.IsDefined(typeof(Protocols.MessageIds), value))
                throw new InvalidDataException($"Invalid message id: {value}.");
            MessageId = (Protocols.MessageIds)value;
        }
    }
}
