using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Moesocks
{
    public enum AddressType : byte
    {
        IPv4 = 1,
        IPv6 = 4,
        HostName = 3
    }

    public class Packet
    {
        public AddressType AddressType { get; set; }
        public IPAddress Address { get; set; }
        public string HostName { get; set; }
        public int Port { get; set; }
    }
}
