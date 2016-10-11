using System;
using System.Collections.Generic;
using Http2.Codec;
using Http2.Headers;

namespace Http2.Frames
{
    /// <summary>
    ///     Conveys configuration parameters that affect how endpoints communicate, such as preferences and constraints on peer
    ///     behavior.
    /// </summary>
    public class SettingsFrame : HttpFrame
    {
        Dictionary<short,int> _settings = new Dictionary<short, int>();

        public SettingsFrame() : base(4)
        {
        }

        /// <summary>
        ///     this frame acknowledges receipt and application of the peer's SETTINGS frame.
        /// </summary>
        public bool IsAcknowledgment { get; set; }

        public void Add(short identifier, int value)
        {
        }

        public void Encode(int streamIdentifier, EncodingContext context)
        {
            if (streamIdentifier != 0)
                throw new DecoderException(Http2ErrorCode.ProtocolError,
                    "SETTINGS frame should not have a stream identifier. You specified: " + streamIdentifier);

            if (IsAcknowledgment)
            {
                context.Buffer[context.Offset++] = 1;
                context.FreeBytesLeftInBuffer--;
                return;
            }

            context.Buffer[context.Offset++] = 0;
            foreach (var setting in _settings)
            {
                context.Buffer[context.Offset++] = (byte) setting.Key;
                context.Buffer[context.Offset++] = (byte)(setting.Key & 0xff);
                context.Buffer[context.Offset++] = (byte)(setting.Value);
                context.Buffer[context.Offset++] = (byte)(setting.Value & 0xff);
                context.Buffer[context.Offset++] = (byte)(setting.Value& 0xff00);
                context.Buffer[context.Offset++] = (byte)(setting.Value & 0xff0000);
            }
            context.FreeBytesLeftInBuffer -= 6*_settings.Count;
        }
    }
}