using System.Threading.Tasks;
using Http2.Frames;

namespace Http2.Codec
{
    internal interface IFrameEncoder
    {
        Task EncodeAsync(EncodingContext context, HttpFrame frame);
    }
}