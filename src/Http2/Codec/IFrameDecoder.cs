using System.Threading.Tasks;
using Http2.Frames;

namespace Http2.Codec
{
    public interface IFrameDecoder
    {
        Task<HttpFrame> DecodeAsync(DecodingContext context, int payloadLength, byte flags, int streamIdentifier);
    }
}