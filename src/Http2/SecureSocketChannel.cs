using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Http2
{
    public class SecureSocketChannel : ISocketChannel
    {
        private Socket _socket;
        private NetworkStream _networkStream;
        private SslStream _sslStream;
        private SemaphoreSlim _connectSynchronization = new SemaphoreSlim(0,1);
        private SocketAsyncEventArgs _connectSocketAsyncEventArgs = new SocketAsyncEventArgs();
        private Exception _connectException;
        const string AlpnHandshake = "h2";

        public SecureSocketChannel()
        {
            _connectSocketAsyncEventArgs.Completed += OnConnectCompleted;
        }

        private void OnConnectCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
                _connectException = new SocketException((int) e.SocketError);
            _connectSynchronization.Release();
        }

        public Task SendAsync(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public Task<int> ReceiveAsync(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public async Task ConnectAsync(string host, int port)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _connectSocketAsyncEventArgs.RemoteEndPoint = new DnsEndPoint(host, port);
            var isPending = _socket.ConnectAsync(_connectSocketAsyncEventArgs);
            if (isPending)
                await _connectSynchronization.WaitAsync();
            if (_connectException != null)
                throw _connectException;
            _networkStream = new NetworkStream(_socket);
            _sslStream = new SslStream(_networkStream);
            await _sslStream.AuthenticateAsClientAsync(host);
        }
    }
}