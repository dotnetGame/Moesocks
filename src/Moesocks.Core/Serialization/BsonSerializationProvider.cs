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

        public async Task<T> Deserialize<T>(Stream stream)
        {
            return (T)await Deserialize(typeof(T), stream);
        }

        private readonly byte[] _lengthBuf = new byte[4];
        public async Task<object> Deserialize(Type type, Stream stream)
        {
            await stream.ReadExactAsync(_lengthBuf, 0, 4);
            var data = new byte[BitConverter.ToUInt32(_lengthBuf, 0)];
            int offset = 0, rest = data.Length;
            while (rest != 0)
            {
                var read = await stream.ReadAsync(data, offset, rest);
                offset += read;
                rest -= read;
            }
            using (var reader = new BsonDataReader(new MemoryStream(data)))
            {
                return _serializer.Deserialize(reader, type);
            }
        }

        public async Task Serialize(object message, Stream stream)
        {
            using (var memStream = new MemoryStream())
            {
                using (var bw = new BinaryWriter(memStream, Encoding.UTF8, true))
                    bw.Write(0u);
                using (var writer = new BsonDataWriter(memStream) { CloseOutput = false })
                {
                    _serializer.Serialize(writer, message);
                }
                var data = memStream.ToArray();
                using (var bw = new BinaryWriter(new MemoryStream(data)))
                    bw.Write(data.Length - 4);
                await stream.WriteAsync(data, 0, data.Length);
            }
        }
    }
}
