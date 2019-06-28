using System;
using System.Collections.Generic;
using System.Text;

namespace Moesocks.Protocol.Messages
{
    [Message(Protocols.MessageType.TcpContent, 1)]
    public class TcpContentMessage
    {
        public string Host;
        public ushort Port;
        public byte[] Content;
    }

    [Message(Protocols.MessageType.TcpEndOfFile, 1)]
    public class TcpEndOfFileMessage
    {
        public string Host;
        public ushort Port;
    }

    [Message(Protocols.MessageType.TcpError, 1)]
    public class TcpErrorMessage
    {
    }
}
