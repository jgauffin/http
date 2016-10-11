using System.Linq;
using System.Text;

namespace Http2
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var buf = Encoding.UTF8.GetBytes("PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n");
            var str = string.Join(", ", buf);
            var buffer = new byte[100];

            var offset = 0;
            var count = buffer.Length;
            IntegerCodec.Encode(7, 1337, buffer, ref offset, ref count);

            offset = 0;
            count = buffer.Length;
            var result = IntegerCodec.Decode(7, buffer, ref offset, ref count);
        }
    }

    public enum DecoderState
    {
        ReadHeader
    }
}