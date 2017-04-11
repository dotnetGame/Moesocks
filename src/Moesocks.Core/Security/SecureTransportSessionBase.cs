using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tomato.Threading;

namespace Moesocks.Security
{
    public enum SecureTransportSessionState
    {
        Disconnected,
        Connecting,
        Connected,
        Error
    }

    public abstract class SecureTransportSessionBase : Stream
    {
        public SecureTransportSessionState State { get; private set; } = SecureTransportSessionState.Disconnected;

        public override bool CanRead => _netStream.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => _netStream.CanWrite;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        private readonly TcpClient _tcpClient;
        private Stream _netStream;
        private readonly OperationQueue _readOperationQueue = new OperationQueue(1);
        private readonly OperationQueue _writeOperationQueue = new OperationQueue(1);
        private readonly SecurePrefix _securePrefix;

        public TcpClient Client => _tcpClient;

        public SecureTransportSessionBase(TcpClient tcpClient, ushort maxRandomBytesLength)
        {
            _securePrefix = new SecurePrefix(maxRandomBytesLength);
            _tcpClient = tcpClient;
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            return _writeOperationQueue.Queue(async () =>
            {
                await Connect();
                await DispatchWriteAsync(buffer, offset, count, token);
            });
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            return _readOperationQueue.Queue(async () =>
            {
                await Connect();
                return await DispatchReadAsync(buffer, offset, count, token);
            });
        }

        private async Task DispatchWriteAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            try
            {
                await _securePrefix.WriteAsync(_netStream);
                await _netStream.WriteAsync(buffer, offset, count, token);
            }
            catch
            {
                State = SecureTransportSessionState.Error;
                throw;
            }
        }

        private async Task<int> DispatchReadAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            try
            {
                await _securePrefix.ReadAsync(_netStream);
                return await _netStream.ReadAsync(buffer, offset, count, token);
            }
            catch
            {
                State = SecureTransportSessionState.Error;
                throw;
            }
        }

        private async Task Connect()
        {
            if (State == SecureTransportSessionState.Connected ||
                State == SecureTransportSessionState.Connecting)
                return;
            try
            {
                State = SecureTransportSessionState.Disconnected;
                if (!_tcpClient.Connected)
                    throw new ObjectDisposedException("tcpclient");
                State = SecureTransportSessionState.Connecting;
                _netStream = await AuthenticateAsync();
                State = SecureTransportSessionState.Connected;
            }
            catch
            {
                State = SecureTransportSessionState.Error;
                throw;
            }
        }

        protected abstract Task<Stream> AuthenticateAsync();

        public override void Flush()
        {
            _writeOperationQueue.Queue(() =>
            {
                _netStream.Flush();
            }).Wait();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _writeOperationQueue.Queue(() =>
            {
                _netStream.Flush();
            });
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer, offset, count).Result;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            WriteAsync(buffer, offset, count).Wait();
        }
    }
}
