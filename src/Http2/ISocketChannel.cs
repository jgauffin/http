using System.Threading.Tasks;

namespace Http2
{
    public interface ISocketChannel
    {
        Task SendAsync(byte[] buffer, int offset, int count);
        Task<int> ReceiveAsync(byte[] buffer, int offset, int count);
        Task ConnectAsync(string host, int port);
    }
}