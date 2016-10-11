using System.Threading.Tasks;

namespace Http2.Codec
{
    public class EncodingContext
    {
        private readonly ISocketChannel _channel;
        private readonly int _bufferCapacity = 65535;
        private readonly int _bufferOffset = 0;
        public byte[] Buffer = new byte[65535];
        public int FreeBytesLeftInBuffer;
        public int Offset;

        public EncodingContext(ISocketChannel channel)
        {
            _channel = channel;
        }

        public bool ContainsData { get { return Offset != _bufferOffset; } }

        public async Task SendAsync()
        {
            await _channel.SendAsync(Buffer, 0, Offset);
            FreeBytesLeftInBuffer = _bufferCapacity;
            Offset = _bufferOffset;
        }
    }
}