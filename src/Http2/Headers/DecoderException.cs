using System;
using System.Runtime.Serialization;

namespace Http2.Headers
{
    [Serializable]
    public class DecoderException : Http2Exception
    {
     

        public DecoderException(int httpErrorCode, string message) : base(httpErrorCode, message)
        {
        }

        public DecoderException(Http2ErrorCode httpErrorCode, string message) : base(httpErrorCode, message)
        {
        }


        public DecoderException(int httpErrorCode, string message, Exception inner) : base(httpErrorCode,message, inner)
        {
        }

        protected DecoderException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}