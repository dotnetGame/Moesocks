using Moesocks.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Moesocks.Server
{
    class TcpClientSession
    {
        private readonly TcpClient _client;
        private readonly Cipher _cipher;
        private readonly byte[] _clientBuffer = new byte[32767];
        private readonly byte[] _remoteBuffer = new byte[32767];
        private Security.CryptoStream _reader;
        private Security.CryptoStream _writer;

        public TcpClientSession(TcpClient client)
        {
            _client = client;
            _cipher = Cipher.Create("aes-256-cfb", "123456789");
        }

        public async void Reset()
        {
            var netStream = _client.GetStream();
            byte[] iv = new byte[_cipher.IVSize];
            if (await netStream.ReadAsync(iv, 0, iv.Length) != iv.Length)
                throw new InvalidDataException();

            var sendIV = _cipher.GenerateIV();
            await netStream.WriteAsync(sendIV, 0, sendIV.Length);

            _reader = new Security.CryptoStream(netStream, _cipher.CreateDecryptor(iv), CryptoStreamMode.Read);
            _writer = new Security.CryptoStream(netStream, _cipher.CreateEncryptor(sendIV), CryptoStreamMode.Write);

            StartReceive();
        }

        private async void StartReceive()
        {
            var packet = new Packet();
            packet.AddressType = (AddressType)await ReadByte();
            switch (packet.AddressType)
            {
                case AddressType.IPv4:
                    packet.Address = new IPAddress(await ReadUInt32());
                    break;
                case AddressType.IPv6:
                    packet.Address = new IPAddress(await ReadBytes(16));
                    break;
                case AddressType.HostName:
                    packet.HostName = await ReadString(await ReadByte());
                    break;
                default:
                    throw new InvalidDataException();
            }
            packet.Port = await ReadUInt16();
            HandleClientIncomingStream(packet);
        }

        private async void HandleClientIncomingStream(Packet packet)
        {
            var address = packet.AddressType == AddressType.HostName ? packet.HostName : packet.Address.ToString();
            Console.WriteLine($"Connecting Client: {(IPEndPoint)_client.Client.RemoteEndPoint}, Remote: {address}:{packet.Port}");

            try
            {
                var remote = new TcpClient();
                {
                    if (packet.AddressType == AddressType.HostName)
                        await remote.ConnectAsync(packet.HostName, packet.Port);
                    else
                        await remote.ConnectAsync(packet.Address, packet.Port);

                    var remoteStream = remote.GetStream();
                    StartReceiveRemoteStream(remoteStream, packet, remote.Client.RemoteEndPoint);

                    int len = 0;
                    while ((len = await _reader.ReadSomeAsync(_clientBuffer, 0, 4096)) != 0)
                    {
                        await remoteStream.WriteAsync(_clientBuffer, 0, len);
                    }
                    await remoteStream.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                _client.Close();
            }
        }

        private void StartReceiveRemoteStream(Stream remoteStream, Packet packet, EndPoint endPoint)
        {
            ThreadPool.QueueUserWorkItem(async o =>
            {
                try
                {
                    int len = 0;
                    while ((len = await remoteStream.ReadAsync(_remoteBuffer, 0, 4096)) != 0)
                    {
                        await _writer.WriteAsync(_remoteBuffer, 0, len);
                    }
                    _writer.FlushFinalBlock();
                }
                catch (ObjectDisposedException) { }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    _client.Close();
                }
            });
        }

        private async Task<byte> ReadByte()
        {
            if (await _reader.ReadAsync(_clientBuffer, 0, 1) != 1)
                throw new InvalidDataException();
            return _clientBuffer[0];
        }

        private async Task<ushort> ReadUInt16()
        {
            if (await _reader.ReadAsync(_clientBuffer, 0, sizeof(ushort)) != sizeof(ushort))
                throw new InvalidDataException();
            return ReverseBytes(BitConverter.ToUInt16(_clientBuffer, 0));
        }

        private async Task<uint> ReadUInt32()
        {
            if (await _reader.ReadAsync(_clientBuffer, 0, sizeof(UInt32)) != sizeof(UInt32))
                throw new InvalidDataException();
            return ReverseBytes(BitConverter.ToUInt32(_clientBuffer, 0));
        }

        private async Task<byte[]> ReadBytes(int count)
        {
            if (await _reader.ReadAsync(_clientBuffer, 0, count) != count)
                throw new InvalidDataException();
            var result = new byte[count];
            Array.Copy(_clientBuffer, result, result.Length);
            return result;
        }

        private async Task<string> ReadString(int count)
        {
            if (await _reader.ReadAsync(_clientBuffer, 0, count) != count)
                throw new InvalidDataException();
            return Encoding.UTF8.GetString(_clientBuffer, 0, count);
        }

        public static UInt16 ReverseBytes(UInt16 value)
        {
            return (UInt16)((value & 0xFFU) << 8 | (value & 0xFF00U) >> 8);
        }

        public static UInt32 ReverseBytes(UInt32 value)
        {
            return (value & 0x000000FFU) << 24 | (value & 0x0000FF00U) << 8 |
                   (value & 0x00FF0000U) >> 8 | (value & 0xFF000000U) >> 24;
        }

        public static UInt64 ReverseBytes(UInt64 value)
        {
            return (value & 0x00000000000000FFUL) << 56 | (value & 0x000000000000FF00UL) << 40 |
                   (value & 0x0000000000FF0000UL) << 24 | (value & 0x00000000FF000000UL) << 8 |
                   (value & 0x000000FF00000000UL) >> 8 | (value & 0x0000FF0000000000UL) >> 24 |
                   (value & 0x00FF000000000000UL) >> 40 | (value & 0xFF00000000000000UL) >> 56;
        }
    }
}
