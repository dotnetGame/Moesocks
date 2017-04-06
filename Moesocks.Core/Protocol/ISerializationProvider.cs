using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Moesocks.Protocol
{
    public interface ISerializationProvider
    {
        Task Serialize(object message, Stream stream);

        Task<T> Deserialize<T>(Stream stream);
    }
}
