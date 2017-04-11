using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Moesocks.Security;

namespace Moesocks.Protocol
{
    public class MessageSerializerSettings
    {
        public static MessageSerializerSettings Default { get; } = new MessageSerializerSettings
        {
            SerializationProvider = new Moesocks.Serialization.BsonSerializationProvider()
        };

        public ISerializationProvider SerializationProvider { get; set; }
    }

    public class MessageSerializer
    {
        public const ushort SerializerVersion = 1;

        private readonly ISerializationProvider _serializationProvider;
        private static readonly Dictionary<Protocols.MessageType, Type> _messageIdToTypes;

        static MessageSerializer()
        {
            _messageIdToTypes = (from t in typeof(MessageAttribute).GetTypeInfo().Assembly.DefinedTypes
                                 where t.IsClass && t.Namespace == "Moesocks.Protocol.Messages" &&
                                 t.IsDefined(typeof(MessageAttribute), false)
                                 let attr = t.GetCustomAttribute<MessageAttribute>(false)
                                 select new { attr.Id, Type = t.AsType() }).ToDictionary(o => o.Id, o => o.Type);
        }

        public MessageSerializer(MessageSerializerSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            if (settings.SerializationProvider == null)
                throw new ArgumentNullException(nameof(settings.SerializationProvider));

            _serializationProvider = settings.SerializationProvider;
        }

        public MessageSerializer()
            : this(MessageSerializerSettings.Default)
        {

        }

        public async Task Serialize(uint identifier, object message, SecureTransportSessionBase stream)
        {
            var attr = message.GetType().GetTypeInfo().GetCustomAttribute<MessageAttribute>();
            if (attr == null)
                throw new InvalidOperationException("message doesn't defined a MessageAttribute.");

            using (var packet = stream.BeginWritePacket())
            {
                await SerializePacket(new PacketHeader
                {
                    ProtocolVersion = Protocols.ProtocolVersion,
                    MessageType = attr.Id,
                    MessageVersion = attr.Version,
                    Identifier = identifier
                }, message, stream);
            }
        }

        public async Task<(uint id, object mesage)> Deserialize(SecureTransportSessionBase stream)
        {
            using (var packet = stream.BeginReadPacket())
            {
                (var header, var message) = await DeserializePacket(stream);
                return (header.Identifier, message);
            }
        }

        private async Task SerializePacket(PacketHeader header, object message, Stream stream)
        {
            using (var headerStream = new MemoryStream())
            {
                using (var bw = new BinaryWriter(headerStream, Encoding.UTF8, true))
                {
                    bw.Write(header.ProtocolVersion);
                    bw.Write((ushort)header.MessageType);
                    bw.Write(header.MessageVersion);
                    bw.Write(header.Identifier);
                }
                var headerBin = headerStream.ToArray();
                await stream.WriteAsync(headerBin, 0, headerBin.Length);
            }
            await _serializationProvider.Serialize(message, stream);
            await stream.FlushAsync();
        }

        private readonly byte[] _headerReadBuf = new byte[10];
        private async Task<(PacketHeader header, object message)> DeserializePacket(Stream stream)
        {
            var header = new PacketHeader();
            await stream.ReadAsync(_headerReadBuf, 0, _headerReadBuf.Length);
            using (var br = new BinaryReader(new MemoryStream(_headerReadBuf)))
            {
                header.VerifyAndSetProtocolVersion(br.ReadUInt16());
                header.VerifyAndSetMessageType(br.ReadUInt16());
                header.MessageVersion = br.ReadUInt16();
                header.Identifier = br.ReadUInt32();
            }
            var message = await DeserializeMessageById(header.MessageType, header.MessageVersion, stream);
            return (header, message);
        }

        private Task<object> DeserializeMessageById(Protocols.MessageType messageId, ushort messageVersion, Stream stream)
        {
            if (!_messageIdToTypes.TryGetValue(messageId, out var messageType))
                throw new NotImplementedException($"Message type for Id: {messageId} is not defined.");
            else
            {
                var expectedVersion = messageType.GetTypeInfo().GetCustomAttribute<MessageAttribute>().Version;
                if (expectedVersion != messageVersion)
                    throw new InvalidDataException($"Invalid message version: {messageVersion}, expected: {expectedVersion}.");
                return _serializationProvider.Deserialize(messageType, stream);
            }
        }
    }
}
