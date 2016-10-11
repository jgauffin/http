using System;
using System.Runtime.Serialization;

namespace Http2.Headers
{
    [Serializable]
    public class Http2Exception : Exception
    {

        public Http2Exception(int httpErrorCode, string message) : base(message)
        {
        }

        public Http2Exception(Http2ErrorCode httpErrorCode, string message) : base(message)
        {
        }

        public Http2Exception(int httpErrorCode, string message, Exception inner) : base(message, inner)
        {
        }

        protected Http2Exception(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}