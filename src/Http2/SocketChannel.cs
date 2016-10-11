using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Http2
{
    public class SocketChannel : ISocketChannel
    {
        private readonly SemaphoreSlim _pendingWriteSynchronization = new SemaphoreSlim(0, 1);
        private readonly SemaphoreSlim _pendingReadSynchronization = new SemaphoreSlim(0, 1);
        private Socket _socket;
        private readonly SocketAsyncEventArgs _writeArgs = new SocketAsyncEventArgs();
        private readonly SocketAsyncEventArgs _readArgs = new SocketAsyncEventArgs();
        private readonly SemaphoreSlim _writeSynchronization = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _readSynchronization = new SemaphoreSlim(1, 1);
        private Exception _writeException;

        public SocketChannel()
        {
            _writeArgs.Completed += OnWriteCompleted;
            _readArgs.Completed += OnReadCompleted;
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true,
                SendBufferSize = 0,
                ReceiveBufferSize = 0
            };
        }

        private void OnReadCompleted(object sender, SocketAsyncEventArgs e)
        {
            _pendingReadSynchronization.Release();
            _pendingWriteSynchronization.Release();
        }

        public async Task SendAsync(byte[] buffer, int offset, int count)
        {
            await _writeSynchronization.WaitAsync();
            _writeArgs.UserToken = count - offset;
            _writeArgs.SetBuffer(buffer, offset, count);
            var isPending = _socket.SendAsync(_writeArgs);
            if (!isPending)
                return;
            await _pendingWriteSynchronization.WaitAsync();
            var exception = _writeException;
            _writeSynchronization.Release();
            if (exception != null)
                throw exception;
        }

        public async Task<int> ReceiveAsync(byte[] buffer, int offset, int count)
        {
            await _readSynchronization.WaitAsync();
            _readArgs.SetBuffer(buffer, offset, count);
            var isPending = _socket.SendAsync(_readArgs);
            if (isPending)
                await _pendingReadSynchronization.WaitAsync();

            if (_readArgs.BytesTransferred == 0)
                throw new SocketException((int)SocketError.NotConnected);
            var bytesTransferred = _readArgs.BytesTransferred;
            _writeSynchronization.Release();

            return bytesTransferred;
        }

        private void OnWriteCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred == 0)
            {
                if (_writeArgs.RemoteEndPoint != null)
                {
                    if (e.SocketError != SocketError.Success)
                        _writeException = new SocketException((int)SocketError.ConnectionReset);
                    _pendingWriteSynchronization.Release();
                }

                var error = e.SocketError != 0 ? e.SocketError : SocketError.ConnectionReset;
                _writeException = new SocketException((int)error);
                _pendingWriteSynchronization.Release();
                return;
            }

            var expectedBytes = (int)e.UserToken;
            if (e.BytesTransferred != expectedBytes)
            {
                _writeException =
                    new ChannelException("Expected " + expectedBytes + " bytes to be written, but only " +
                                         e.BytesTransferred + " bytes was written");
                _pendingWriteSynchronization.Release();
                return;
            }

            _pendingWriteSynchronization.Release();
        }

        public async Task ConnectAsync(string remoteHost, int port)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _writeArgs.RemoteEndPoint = new DnsEndPoint(remoteHost, port);
            var isPending = _socket.ConnectAsync(_writeArgs);
            if (isPending)
                await _writeSynchronization.WaitAsync();
            _writeArgs.RemoteEndPoint = null;
            if (_writeException != null)
                throw _writeException;
        }

        public void Assign(Socket clientSocket)
        {
            if (clientSocket == null) throw new ArgumentNullException(nameof(clientSocket));
            _socket = clientSocket;
        }
    }
}