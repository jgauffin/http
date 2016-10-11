using Http2.Headers;

namespace Http2.Codec
{
    internal class EncoderException : Http2Exception
    {
        public EncoderException(Http2ErrorCode errorCode, string errorMessage) : base(errorCode, errorMessage)
        {
        }
    }
}