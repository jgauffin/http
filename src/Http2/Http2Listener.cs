using System.Net;
using System.Net.Sockets;

namespace Http2
{
    public class Http2Listener
    {
        private readonly SocketAsyncEventArgs _listenArgs = new SocketAsyncEventArgs();
        private Socket _listenerSocket;

        public int Backlog { get; set; }

        public void Start(IPAddress address, int port)
        {
            _listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listenerSocket.Bind(new IPEndPoint(address, port));
            _listenerSocket.Listen(Backlog);
            var isPending = _listenerSocket.AcceptAsync(_listenArgs);
            if (!isPending)
            {
                AcceptConnection(_listenArgs);
            }
        }

        private void AcceptConnection(SocketAsyncEventArgs listenArgs)
        {
            if (listenArgs.SocketError != SocketError.Success)
            {
                //TODO: ERROR
            }

            var clientSocket = listenArgs.AcceptSocket;
            var channel = new Http2Channel();
            channel.ActAsServerAsync(clientSocket);
        }
    }
}