using System;
using System.Collections.Generic;
using System.Text;

namespace Moesocks.Protocol.Messages
{
    [Message(Protocols.MessageType.HttpRequest, 1)]
    public class HttpRequestMessage
    {
        public string Uri;
        public string Method;
        public Dictionary<string, string> Headers;
        public byte[] Body;
    }

    [Message(Protocols.MessageType.HttpResponse, 1)]
    public class HttpResponseMessage
    {
        public uint StatusCode;
        public Dictionary<string, string> Headers;
        public byte[] Body;
    }
}
