using System;
using System.Collections.Generic;
using System.Text;

namespace Moesocks.Protocol
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class MessageAttribute : Attribute
    {
        public Protocols.MessageType Id { get; }
        public ushort Version { get; }

        public MessageAttribute(Protocols.MessageType id, ushort version)
        {
            Id = id;
            Version = version;
        }
    }
}
