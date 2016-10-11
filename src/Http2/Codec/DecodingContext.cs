using System.Threading.Tasks;

namespace Http2.Codec
{
    public sealed class DecodingContext
    {
        private readonly ISocketChannel _channel;
        private readonly int _bufferCapacity = 65535;
        private readonly int _bufferOffset = 0;
        public byte[] Buffer = new byte[65535];
        public int BytesLeftToProcess;
        public int Offset;

        public DecodingContext(ISocketChannel channel)
        {
            _channel = channel;
        }

        public int Capacity { get { return _bufferCapacity; } }

        public async Task ReceiveMoreAsync()
        {
            if (BytesLeftToProcess > 0 && Offset > _bufferOffset)
            {
                System.Buffer.BlockCopy(Buffer, Offset, Buffer, _bufferOffset, BytesLeftToProcess);
                Offset = _bufferOffset;
            }

            BytesLeftToProcess =
                await _channel.ReceiveAsync(Buffer, Offset + BytesLeftToProcess, _bufferCapacity - BytesLeftToProcess);
        }

        public async Task ReceiveMoreAsync(int minimumAvailableBytes)
        {
            while (BytesLeftToProcess < minimumAvailableBytes)
            {
                await ReceiveMoreAsync();
            }
        }
    }
}