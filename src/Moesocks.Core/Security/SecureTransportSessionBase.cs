using Microsoft.Extensions.Logging;
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

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        private readonly TcpClient _tcpClient;
        private Stream _netStream;
        private readonly SecurePrefix _securePrefix;
        private uint _readSurfix = 0;
        private uint _writeSurfix = 0;

        public SecureTransportSessionBase(ushort maxRandomBytesLength, ILoggerFactory loggerFactory)
        {
            _securePrefix = new SecurePrefix(maxRandomBytesLength, loggerFactory);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            await Connect();
            await DispatchWriteAsync(buffer, offset, count, token);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            await Connect();
            return await DispatchReadAsync(buffer, offset, count, token);
        }

        private async Task DispatchWriteAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            try
            {
                if (_writeSurfix != 0)
                {
                    await _securePrefix.WriteAsync(_netStream);
                    --_writeSurfix;
                }
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
                if (_readSurfix != 0)
                {
                    await _securePrefix.ReadAsync(_netStream);
                    _readSurfix--;
                }
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

        internal BeginWritePacketScope BeginWritePacket()
        {
            return new BeginWritePacketScope(this);
        }

        internal BeginReadPacketScope BeginReadPacket()
        {
            return new BeginReadPacketScope(this);
        }

        internal struct BeginWritePacketScope : IDisposable
        {
            private readonly SecureTransportSessionBase _session;
            public BeginWritePacketScope(SecureTransportSessionBase session)
            {
                _session = session;
                _session._writeSurfix++;
            }

            public void Dispose()
            {

            }
        }

        internal struct BeginReadPacketScope : IDisposable
        {
            private readonly SecureTransportSessionBase _session;
            public BeginReadPacketScope(SecureTransportSessionBase session)
            {
                _session = session;
                _session._readSurfix++;
            }

            public void Dispose()
            {

            }
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _netStream.FlushAsync();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
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
            throw new NotSupportedException();
        }
    }
}
