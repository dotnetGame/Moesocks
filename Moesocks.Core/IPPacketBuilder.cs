using System;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Moesocks
{
    public struct IPPacketBuilder
    {
        public ushort Id;
        public IPAddress Source;
        public IPAddress Destination;
        public byte TTL;
        public byte Protocol;
        public ArraySegment<byte> Payload;

        private const byte _VersionHeaderLen = 0x45;

        public unsafe int Build(ArraySegment<byte> buffer)
        {
            fixed (byte* dest = buffer.Array)
            {
                ref var header = ref Unsafe.AsRef<IPPacketHeader>(dest + buffer.Offset);
                header.VersionHeaderLen = _VersionHeaderLen;
                header.Service = 0;
                header.Id = ToBigEndian(Id);
                header.FlagsFragmentOffset = 0;
                header.TTL = TTL;
                header.Protocol = Protocol;
                header.Source = ToBigEndian(Source);
                header.Destination = ToBigEndian(Destination);
                header.AutoFillCheckSum();

                var offset = buffer.Offset + Unsafe.SizeOf<IPPacketHeader>();
                fixed (byte* src = Payload.Array)
                    Buffer.MemoryCopy(src + Payload.Offset, dest + offset, buffer.Count - offset, Payload.Count);
                var length = (ushort)(Unsafe.SizeOf<IPPacketHeader>() + Payload.Count);
                header.Length = ToBigEndian(length);
                return length;
            }
        }

        public static ushort ToBigEndian(ushort value)
        {
            if (BitConverter.IsLittleEndian)
                return unchecked((ushort)((value >> 8) | (value << 8)));
            return value;
        }

        public static uint ToBigEndian(IPAddress address)
        {
            var bytes = address.GetAddressBytes();
            //if (BitConverter.IsLittleEndian)
            //    Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes, 0);
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct IPPacketHeader
    {
        /// <summary>
        /// 版本 + 首部长度
        /// </summary>
        public byte VersionHeaderLen;

        /// <summary>
        /// 区分服务
        /// </summary>
        public byte Service;

        /// <summary>
        /// 总长度
        /// </summary>
        public ushort Length;

        /// <summary>
        /// 标识
        /// </summary>
        public ushort Id;

        /// <summary>
        /// 标志 + 片偏移
        /// </summary>
        public ushort FlagsFragmentOffset;

        /// <summary>
        /// 生存时间
        /// </summary>
        public byte TTL;

        /// <summary>
        /// 协议
        /// </summary>
        public byte Protocol;

        /// <summary>
        /// 首部校验和
        /// </summary>
        public ushort CheckSum;

        /// <summary>
        /// 源地址
        /// </summary>
        public uint Source;

        /// <summary>
        /// 目的地址
        /// </summary>
        public uint Destination;

        public unsafe void AutoFillCheckSum()
        {
            CheckSum = 0;
            var p = (ushort*)Unsafe.AsPointer(ref this);
            var times = Unsafe.SizeOf<IPPacketHeader>() / sizeof(ushort);
            ushort checkSum = 0;
            for (int i = 0; i < times; i++)
            {
                unchecked
                {
                    checkSum += ToLittleEndian((ushort)~*p++);
                }
            }
            CheckSum = IPPacketBuilder.ToBigEndian((ushort)~checkSum);
        }

        public static ushort ToLittleEndian(ushort value)
        {
            if (!BitConverter.IsLittleEndian)
                return unchecked((ushort)((value >> 8) | (value << 8)));
            return value;
        }
    }
}
