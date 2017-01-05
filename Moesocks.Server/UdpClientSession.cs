using Moesocks.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Moesocks.Server
{
    class UdpClientSession
    {
        private readonly UdpClient _client, _remote;
        private readonly Cipher _cipher;

        public UdpClientSession(UdpClient udpClient)
        {
            _client = udpClient;
            _remote = new UdpClient(AddressFamily.InterNetwork);
            _cipher = Cipher.Create("aes-256-cfb", "123456789");
        }

        public void Start()
        {
            ThreadPool.QueueUserWorkItem(async o =>
            {
                while (true)
                {
                    try
                    {
                        await Dispatch(await _client.ReceiveAsync());
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
            });
        }

        private async Task Dispatch(UdpReceiveResult result)
        {
            var iv = new byte[16];
            Array.Copy(result.Buffer, iv, iv.Length);

            var reader = new Security.CryptoStream(new MemoryStream(result.Buffer, iv.Length, result.Buffer.Length - iv.Length, false),
                _cipher.CreateDecryptor(iv), CryptoStreamMode.Read);

            using (var clientStream = new MemoryStream())
            {
                var writer = new System.Security.Cryptography.CryptoStream(clientStream, _cipher.CreateEncryptor(iv), CryptoStreamMode.Write);
                var buffer = new byte[32767];
                var len = await reader.ReadAsync(buffer, 0, 32767);
                await _remote.SendAsync(buffer, len);
                var remoteResult = await _remote.ReceiveAsync();
                await writer.WriteAsync(remoteResult.Buffer, 0, remoteResult.Buffer.Length);

                var array = clientStream.ToArray();
                await _client.SendAsync(array, array.Length);
            }
        }
    }
}
