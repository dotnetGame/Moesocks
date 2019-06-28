using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Moesocks.Security
{
    public abstract class SecureTransportSessionBase : Stream
    {
        private Stream _netStream;
        private readonly ILogger _logger;

        public override bool CanRead => _netStream?.CanRead ?? throw new ObjectDisposedException("netStream");

        public override bool CanSeek => _netStream?.CanSeek ?? throw new ObjectDisposedException("netStream");

        public override bool CanWrite => _netStream?.CanWrite ?? throw new ObjectDisposedException("netStream");

        public override long Length => _netStream?.Length ?? throw new ObjectDisposedException("netStream");

        public override bool CanTimeout => _netStream?.CanTimeout ?? throw new ObjectDisposedException("netStream");

        public override int ReadTimeout
        {
            get => _netStream?.ReadTimeout ?? throw new ObjectDisposedException("netStream");
            set
            {
                CheckDisposed();
                _netStream.ReadTimeout = value;
            }
        }

        public override int WriteTimeout
        {
            get => _netStream?.WriteTimeout ?? throw new ObjectDisposedException("netStream");
            set
            {
                CheckDisposed();
                _netStream.WriteTimeout = value;
            }
        }

        public override long Position
        {
            get => _netStream?.Position ?? throw new ObjectDisposedException("netStream");
            set
            {
                CheckDisposed();
                _netStream.Position = value;
            }
        }

        public SecureTransportSessionBase(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SecureTransportSessionBase>();
        }

        public async Task ConnectAsync()
        {
            _netStream = await AuthenticateAsync();
        }

        protected abstract Task<Stream> AuthenticateAsync();

        public override void Flush()
        {
            CheckDisposed();
            _netStream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            CheckDisposed();
            return _netStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            CheckDisposed();
            _netStream.SetLength(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            CheckDisposed();
            return _netStream.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            CheckDisposed();
            _netStream.Write(buffer, offset, count);
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            CheckDisposed();
            return _netStream.BeginRead(buffer, offset, count, callback, state);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            CheckDisposed();
            return _netStream.BeginWrite(buffer, offset, count, callback, state);
        }

        public override void Close()
        {
            _netStream?.Close();
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            CheckDisposed();
            return _netStream.CopyToAsync(destination, bufferSize, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _netStream?.Dispose();
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            CheckDisposed();
            return _netStream.EndRead(asyncResult);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            CheckDisposed();
            _netStream.EndWrite(asyncResult);
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            CheckDisposed();
            return _netStream.FlushAsync(cancellationToken);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            CheckDisposed();
            return _netStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            CheckDisposed();
            return _netStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override void WriteByte(byte value)
        {
            CheckDisposed();
            _netStream.WriteByte(value);
        }

        public override int ReadByte()
        {
            CheckDisposed();
            return _netStream.ReadByte();
        }

        private void CheckDisposed()
        {
            if (_netStream == null)
                throw new ObjectDisposedException("netStream");
        }
    }
}
