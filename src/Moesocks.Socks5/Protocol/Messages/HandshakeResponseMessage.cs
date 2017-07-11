using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Moesocks.Socks5.Protocol.Messages
{
    public sealed class HandshakeResponseMessage
    {
        public byte VER;
        public AuthenticateMethod METHOD;

        public Task WriteToAsync(BinaryWriter writer)
        {
            writer.Write(VER);
            writer.Write((byte)METHOD);
            writer.Flush();
            return Task.CompletedTask;
        }
    }
}
