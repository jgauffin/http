using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Http2.Frames;
using Http2.Headers;

namespace Http2.Codec
{
    /// <summary>
    /// Decodes HTTP frames.
    /// </summary>
    internal class FrameDecoder
    {
        private const int HeaderSize = 9;
        private const int Bit32 = 1 << 32;

        //key = stream identifier.
        private readonly Dictionary<int, ContinuationHandler> _continuationHandlers =
            new Dictionary<int, ContinuationHandler>();

        private readonly Dictionary<int, IFrameDecoder> _extraDecoders = new Dictionary<int, IFrameDecoder>();
        private readonly Decoder _headerDecoder = new Decoder();

        //only set if we need a continuation frame
        private HeaderFrame _headerFrame;

        private ParsedHeaderHandler _headerReceiver;

        public FrameDecoder()
        {
            _headerDecoder.HeaderDecoded += OnHeaderDecoded;
        }

        /// <summary>
        ///     Maximum frame size in bytes.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The size of a frame payload is limited by the maximum size that a receiver advertises in the
        ///         SETTINGS_MAX_FRAME_SIZE setting. This setting can have any value between 214 (16,384) and 224-1 (16,777,215)
        ///         octets, inclusive.
        ///     </para>
        ///     <para>
        ///         All implementations MUST be capable of receiving and minimally processing frames up to 214 octets in length,
        ///         plus the 9-octet frame header (Section 4.1). The size of the frame header is not included when describing frame
        ///         sizes.
        ///     </para>
        /// </remarks>
        public int MaxFrameSize { get; set; }

        /// <summary>
        ///     Decode a header.
        /// </summary>
        /// <param name="context">decoding context</param>
        /// <returns>
        ///     Frame in most cases. But <c>null</c> can be returned if a continuation frame is required and is some other
        ///     specific situations.
        /// </returns>
        public async Task<HttpFrame> DecodeAsync(DecodingContext context)
        {
            if (context.BytesLeftToProcess < HeaderSize)
                await context.ReceiveMoreAsync();

            int payloadLength = context.Buffer[context.Offset++];
            payloadLength += context.Buffer[context.Offset++] << 4;
            payloadLength += context.Buffer[context.Offset++] << 8;
            if (payloadLength > MaxFrameSize)
                throw new DecoderException(Http2ErrorCode.FrameSizeError,
                    $"Received HTTP frame have size {payloadLength} while this server allows maximum {MaxFrameSize} bytes.");

            var type = context.Buffer[context.Offset++];
            var flags = context.Buffer[context.Offset++];
            var streamIdentifier = BitConverter.ToInt32(context.Buffer, context.Offset) & ~Bit32;
            context.BytesLeftToProcess -= HeaderSize;

            if (type == 0x9)
            {
                ContinuationHandler handler;
                if (!_continuationHandlers.TryGetValue(streamIdentifier, out handler))
                    throw new DecoderException(Http2ErrorCode.ProtocolError,
                        "Got a continuation frame without previous frame defining that.");

                _continuationHandlers.Remove(streamIdentifier);
                return handler(context, payloadLength, flags, streamIdentifier);
            }


            if (context.BytesLeftToProcess < payloadLength)
                await context.ReceiveMoreAsync(payloadLength);

            switch (type)
            {
                case 0:
                    return DecodeDataFrame(context, payloadLength, flags, streamIdentifier);
                case 1:
                    return DecodeHeaderFrame(context, payloadLength, flags, streamIdentifier);
                case 2:
                    return DecodePriorityFrame(context, payloadLength, flags, streamIdentifier);
                case 3:
                    return DecodeResetFrame(context, payloadLength, flags, streamIdentifier);
                case 4:
                    return DecodeSettingsFrame(context, payloadLength, flags, streamIdentifier);
                case 5:
                    return DecodePushPromise(context, payloadLength, flags, streamIdentifier);
                case 6:
                    return DecodePing(context, payloadLength, flags, streamIdentifier);
                case 7:
                    return DecodeGoAway(context, payloadLength, flags, streamIdentifier);
                case 8:
                    return DecodeWindowUpdate(context, payloadLength, flags, streamIdentifier);
                case 9:
                    throw new NotSupportedException("Delegate should ahve handled this");

                default:
                    IFrameDecoder decoder;
                    if (!_extraDecoders.TryGetValue(type, out decoder))
                        return null;
                    return await decoder.DecodeAsync(context, payloadLength, flags, streamIdentifier);
            }
        }

        private WindowUpdate DecodeWindowUpdate(DecodingContext context, int payloadLength, byte flags,
            int streamIdentifier)
        {
            if (payloadLength != 8)
                throw new DecoderException(Http2ErrorCode.FrameSizeError, "WINDOW_UPDATE frame payload must be 4 bytes.");

            var windowsSizeIncrement = BitConverter.ToInt32(context.Buffer, context.Offset) & ~Bit32;
            var frame = new WindowUpdate(windowsSizeIncrement);

            context.Offset += 4;
            context.BytesLeftToProcess -= 8;

            return frame;
        }

        private HttpFrame DecodeGoAway(DecodingContext context, int payloadLength, byte flags, int streamIdentifier)
        {
            if (streamIdentifier != 0)
                throw new DecoderException(Http2ErrorCode.ProtocolError,
                    "GOAWAY frame must not have a stream identifier.");

            var lastStreamId = BitConverter.ToInt32(context.Buffer, context.Offset) & ~Bit32;
            var errorCode = BitConverter.ToInt32(context.Buffer, context.Offset + 4);
            context.Offset += 8;
            context.BytesLeftToProcess -= 8;

            var frame = new GoAway(lastStreamId, errorCode);
            if (payloadLength == 8)
                return frame;

            var data = new byte[payloadLength - 8];
            Buffer.BlockCopy(context.Buffer, context.Offset, data, 0, payloadLength - 8);
            frame.DebugData = data;
            context.Offset += data.Length;
            context.BytesLeftToProcess -= data.Length;
            return frame;
        }

        private HttpFrame DecodePing(DecodingContext context, int payloadLength, byte flags, int streamIdentifier)
        {
            if (payloadLength != 8)
                throw new DecoderException(Http2ErrorCode.FrameSizeError, "PING frame payload must be 8 bytes.");
            if (streamIdentifier != 0)
                throw new DecoderException(Http2ErrorCode.ProtocolError,
                    "PING frame must not have a stream identifier.");

            var opaqueData = BitConverter.ToInt64(context.Buffer, context.Offset);
            context.Offset += 8;
            context.BytesLeftToProcess -= 8;
            var isPingResponse = (flags & 1) == 1;
            return new Ping(isPingResponse, opaqueData);
        }

        private void OnHeaderDecoded(object sender, HeaderEventArgs e)
        {
            _headerReceiver(e);
        }

        private PushPromise DecodePushPromise(DecodingContext context, int payloadLength, byte flags,
            int streamIdentifier)
        {
            if (streamIdentifier != 0)
                throw new DecoderException(Http2ErrorCode.ProtocolError,
                    "PUSH_PROMISE frame must have a stream identifier.");

            var paddingSize = GetPaddingSize(context, flags);
            var promisedStreamId = BitConverter.ToInt32(context.Buffer, context.Offset) & ~Bit32;
            context.Offset += 4;
            context.BytesLeftToProcess -= 4;

            var frame = new PushPromise
            {
                FrameFlags = flags,
                StreamIdentifier = streamIdentifier,
                PromisedStreamId = promisedStreamId
            };


            _headerReceiver = args => frame.Add(args.Name, args.Value, args.IsIndexingAllowed);
            _headerDecoder.Decode(context.Buffer, ref context.Offset, ref context.BytesLeftToProcess);
            _headerReceiver = null;

            // skip padding
            context.BytesLeftToProcess -= paddingSize;
            context.Offset += paddingSize;

            return frame;
        }

        private SettingsFrame DecodeSettingsFrame(DecodingContext context, int payloadLength, byte flags,
            int streamIdentifier)
        {
            if (streamIdentifier != 0)
                throw new DecoderException(Http2ErrorCode.ProtocolError,
                    "SETTINGS frame should not have a stream identifier. You specified: " + streamIdentifier);
            if (payloadLength%6 != 0)
                throw new DecoderException(Http2ErrorCode.FrameSizeError,
                    "SETTINGS frame payload length should be a multiple of 6. You specified:" + payloadLength);

            if ((flags & 1) == 1)
            {
                if (payloadLength > 0)
                    throw new DecoderException(Http2ErrorCode.ProtocolError,
                        "SETTINGS frame with the acknowledgment flag may not contain a payload.");
                return new SettingsFrame {IsAcknowledgment = true};
            }

            var originalLength = payloadLength;
            var settingsFrame = new SettingsFrame();
            while (payloadLength > 0)
            {
                var identifier = BitConverter.ToInt16(context.Buffer, context.Offset);
                var value = BitConverter.ToInt32(context.Buffer, context.Offset + 2);
                settingsFrame.Add(identifier, value);
                context.Offset += 6;
                payloadLength -= 6;
            }
            context.BytesLeftToProcess -= originalLength;
            return settingsFrame;
        }

        private ResetStream DecodeResetFrame(DecodingContext context, int payloadLength, byte flags,
            int streamIdentifier)
        {
            if (payloadLength != 4)
                throw new DecoderException(Http2ErrorCode.FrameSizeError,
                    "RST_STREAM frame must have a payload of 4 bytes.");

            var errorCode = BitConverter.ToInt32(context.Buffer, context.Offset);
            return new ResetStream(errorCode) {StreamIdentifier = streamIdentifier, FrameFlags = flags};
        }

        private PriorityFrame DecodePriorityFrame(DecodingContext context, int payloadLength, byte flags,
            int streamIdentifier)
        {
            if (streamIdentifier == 0)
                throw new DecoderException(Http2ErrorCode.ProtocolError, "PRIORITY frames require a stream identifier.");
            if (payloadLength != 5)
                throw new DecoderException(Http2ErrorCode.FrameSizeError, "PRIORITY frame payload should be 5 bytes.");


            var exlusive = (context.Buffer[context.Offset] & Bit32) == Bit32;
            var dependency = BitConverter.ToInt32(context.Buffer, context.Offset) & ~Bit32;
            var weight = context.Buffer[context.Offset + 4];
            context.Offset += 5;
            context.BytesLeftToProcess -= 5;

            var frame = new PriorityFrame
            {
                DependencyStreamId = dependency,
                Weight = weight,
                IsExclusive = exlusive,
                StreamIdentifier = streamIdentifier,
                FrameFlags = flags
            };

            return frame;
        }

        private HeaderFrame DecodeHeaderFrame(DecodingContext context, int payloadLength, byte flags,
            int streamIdentifier)
        {
            var paddingSize = GetPaddingSize(context, flags);
            var isPriority = (flags & 0x20) == 0x20;
            if (isPriority)
            {
                var isExclusive = (context.Buffer[context.Offset] & Bit32) == Bit32;
                var streamDependencyId = BitConverter.ToInt32(context.Buffer, context.Offset) & ~Bit32;
                context.Offset += 4;
                var weight = context.Buffer[context.Offset++];
                context.BytesLeftToProcess -= 5;
            }


            _headerFrame = new HeaderFrame
            {
                FrameFlags = flags,
                StreamIdentifier = streamIdentifier
            };
            _headerReceiver = args => _headerFrame.Add(args.Name, args.Value, args.IsIndexingAllowed);
            _headerDecoder.Decode(context.Buffer, ref context.Offset, ref context.BytesLeftToProcess);
            _headerReceiver = null;

            // skip padding
            context.BytesLeftToProcess -= paddingSize;
            context.Offset += paddingSize;

            // A HEADERS frame without the END_HEADERS flag set MUST be followed by a CONTINUATION frame for the same stream
            if ((flags & 0x4) == 0x4)
            {
                _continuationHandlers[streamIdentifier] = ResumeHeaderFrame;
                return null;
            }

            var frame = _headerFrame;
            _headerFrame = null;
            return frame;
        }

        /// <summary>
        ///     CONTINUATION frame for the HEADERS frame
        /// </summary>
        private HttpFrame ResumeHeaderFrame(DecodingContext context, int contentlength, byte flags, int streamIdentifier)
        {
            if (streamIdentifier == 0)
                throw new DecoderException(Http2ErrorCode.ProtocolError,
                    "CONTINUATION frames require a stream identifier.");

            _headerReceiver = args => _headerFrame.Add(args.Name, args.Value, args.IsIndexingAllowed);
            _headerDecoder.Decode(context.Buffer, ref context.Offset, ref context.BytesLeftToProcess);
            _headerReceiver = null;

            // A HEADERS frame without the END_HEADERS flag set MUST be followed by a CONTINUATION frame for the same stream
            if ((flags & 0x4) == 0x4)
            {
                _continuationHandlers[streamIdentifier] = ResumeHeaderFrame;
                return null;
            }

            var frame = _headerFrame;
            _headerFrame = null;
            return frame;
        }

        private DataFrame DecodeDataFrame(DecodingContext context, int payloadLength, byte flags,
            int streamIdentifier)
        {
            if (streamIdentifier == 0)
                throw new Http2Exception(Http2ErrorCode.ProtocolError, "Data frame without a stream identifier.");

            var paddingSize = GetPaddingSize(context, flags);

            var ms = new MemoryStream {Capacity = payloadLength};
            ms.Write(context.Buffer, context.Offset, payloadLength);
            context.Offset += payloadLength;
            context.BytesLeftToProcess -= payloadLength;

            var frame = new DataFrame
            {
                ContentLength = payloadLength,
                StreamIdentifier = streamIdentifier,
                FrameFlags = flags,
                Data = ms
            };

            // skip padding
            context.BytesLeftToProcess -= paddingSize;
            context.Offset += paddingSize;

            return frame;
        }

        private static int GetPaddingSize(DecodingContext context, byte flags, byte padFlag = 8)
        {
            if ((flags & padFlag) != padFlag)
                return 0;

            int paddingSize = context.Buffer[context.Offset++];
            context.BytesLeftToProcess--;
            return paddingSize;
        }

        private delegate void ParsedHeaderHandler(HeaderEventArgs e);

        private delegate HttpFrame ContinuationHandler(
            DecodingContext context, int contentLength, byte flags, int streamIdentifier);
    }
}