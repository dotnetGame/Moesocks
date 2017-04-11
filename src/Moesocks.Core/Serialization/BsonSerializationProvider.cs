using Moesocks.Protocol;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json;

namespace Moesocks.Serialization
{
    public class BsonSerializationProvider : ISerializationProvider
    {
        private readonly JsonSerializer _serializer;

        public BsonSerializationProvider()
        {
            _serializer = new JsonSerializer();
        }

        public Task<T> Deserialize<T>(Stream stream)
        {
            using (var writer = new BsonDataReader(stream) { CloseInput = false })
            {
                return Task.FromResult(_serializer.Deserialize<T>(writer));
            }
        }

        public Task Serialize(object message, Stream stream)
        {
            using (var writer = new BsonDataWriter(stream) { CloseOutput = false })
            {
                _serializer.Serialize(writer, message);
            }
            return Task.CompletedTask;
        }
    }
}
