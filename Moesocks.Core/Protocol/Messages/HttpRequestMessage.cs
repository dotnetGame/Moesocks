using System;
using System.Collections.Generic;
using System.Text;

namespace Moesocks.Protocol.Messages
{
    public class HttpRequestMessage
    {
        public string Uri;
        public string Method;
        public Dictionary<string, string> Headers;
        public byte[] Body;
    }
}
