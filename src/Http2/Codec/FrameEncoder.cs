using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Http2.Frames;
using Http2.Headers;

namespace Http2.Codec
{
    internal class FrameEncoder
    {
        private const int HeaderSize = 9;
        private const int Bit32 = 1 << 32;
        private readonly EncodingContext _context;
        private readonly Dictionary<int, IFrameEncoder> _extraEncoders = new Dictionary<int, IFrameEncoder>();
        private readonly Encoder _headerEncoder;

        public FrameEncoder(EncodingContext context)
        {
            _context = context;
            _headerEncoder = new Encoder(65535);
        }

        public async Task EncodeAsync(HttpFrame frame)
        {
            if (_context.FreeBytesLeftInBuffer < HeaderSize)
                await _context.SendAsync();


            int payloadLength = _context.Buffer[_context.Offset++];
            payloadLength += _context.Buffer[_context.Offset++] << 4;
            payloadLength += _context.Buffer[_context.Offset++] << 8;
            if (payloadLength > MaxFrameSize)
                throw new EncoderException(Http2ErrorCode.FrameSizeError,
                    $"Received HTTP frame have size {payloadLength} while this server allows maximum {MaxFrameSize} bytes.");

            var type = _context.Buffer[_context.Offset++];
            var flags = _context.Buffer[_context.Offset++];
            var streamIdentifier = BitConverter.ToInt32(_context.Buffer, _context.Offset) & ~Bit32;
            _context.FreeBytesLeftInBuffer -= HeaderSize;

            switch (type)
            {
                case 0:
                    await EncodeDataFrameAsync(_context, frame);
                    break;
                case 1:
                    await EncodeHeaderFrameAsync(_context, frame);
                    break;
                case 2:
                    await EncodePriorityFrameAsync(_context, frame);
                    break;
                case 3:
                    await EncodeResetFrameAsync(_context, frame);
                    break;
                //case 4:
                //    return EncodeSettingsFrameAsync(_context, frame);
                //case 5:
                //    return EncodePushPromiseAsync(_context, frame);
                //case 6:
                //    return EncodePingAsync(_context, frame);
                //case 7:
                //    return EncodeGoAwayAsync(_context, frame);
                //case 8:
                //    return EncodeWindowUpdateAsync(_context, frame);
                case 9:
                    throw new NotSupportedException("Delegate should ahve handled this");

                default:
                    IFrameEncoder decoder;
                    if (!_extraEncoders.TryGetValue(type, out decoder))
                        return;
                    await decoder.EncodeAsync(_context, frame);
                    break;
            }
        }

        private async Task EncodeResetFrameAsync(EncodingContext context, HttpFrame frame)
        {
            if (context.FreeBytesLeftInBuffer < 4 + HeaderSize)
                await context.SendAsync();

            var resetFrame = (ResetStream)frame;
            AddHeader(context, frame.FrameType, 0, 4, frame.StreamIdentifier);
            context.Buffer[context.Offset++] = (byte)resetFrame.ErrorCode;
            context.Buffer[context.Offset++] = (byte)(resetFrame.ErrorCode & 0xff00);
            context.Buffer[context.Offset++] = (byte)(resetFrame.ErrorCode & 0xff0000);
            context.Buffer[context.Offset++] = (byte)(resetFrame.ErrorCode & 0xff000000);
            context.FreeBytesLeftInBuffer -= 4;
        }

        private async Task EncodePriorityFrameAsync(EncodingContext context, HttpFrame frame)
        {
            if (context.FreeBytesLeftInBuffer < 5 + HeaderSize)
                await context.SendAsync();

            var prioFrame = (PriorityFrame)frame;
            AddHeader(context, frame.FrameType, 0, 5, frame.StreamIdentifier);
            context.Buffer[context.Offset++] = (byte)prioFrame.DependencyStreamId;
            context.Buffer[context.Offset++] = (byte)(prioFrame.DependencyStreamId & 0xff00);
            context.Buffer[context.Offset++] = (byte)(prioFrame.DependencyStreamId & 0xff0000);
            context.Buffer[context.Offset++] = (byte)(prioFrame.DependencyStreamId & 0xff000000);
            context.Buffer[context.Offset++] = (byte)prioFrame.Weight;
            context.FreeBytesLeftInBuffer -= 5;
        }

        private async Task EncodeHeaderFrameAsync(EncodingContext context, HttpFrame frame)
        {
            var headerFrame = (HeaderFrame)frame;

            var headerOffset = context.Offset;
            context.Offset += HeaderSize;

            var startOffset = context.Offset;
            foreach (var header in headerFrame)
            {
                //frame header(9) + padLength + streamDependency(4) + weight + header block extras (4)
                var size = header.Name.Length + header.Value.Length + 19;
                if (context.FreeBytesLeftInBuffer < size)
                {
                    var flags2 = frame.IsEndOfStream ? 1 : 0;
                    var payloadLength2 = context.Offset - startOffset;
                    context.Buffer[headerOffset] = (byte)(payloadLength2 & (1 << 8));
                    context.Buffer[headerOffset + 1] = (byte)(payloadLength2 & (1 << 16));
                    context.Buffer[headerOffset + 2] = (byte)(payloadLength2 & (1 << 24));
                    context.Buffer[headerOffset + 3] = frame.FrameType;
                    context.Buffer[headerOffset + 4] = (byte)flags2;
                    context.Buffer[headerOffset + 5] = (byte)(frame.StreamIdentifier & (1 << 8));
                    context.Buffer[headerOffset + 6] = (byte)(frame.StreamIdentifier & (1 << 16));
                    context.Buffer[headerOffset + 7] = (byte)(frame.StreamIdentifier & (1 << 24));
                    context.Buffer[headerOffset + 8] = (byte)(frame.StreamIdentifier & (1 << 32));
                    await context.SendAsync();
                    startOffset = context.Offset + HeaderSize;
                    headerOffset = context.Offset;
                }



                if (header.ContainsSensitiveData)
                    _headerEncoder.EncodeSensitive(header.Name, header.Value, context.Buffer, ref context.Offset, ref context.FreeBytesLeftInBuffer);
                else if (!header.IsIndexingAllowed)
                    _headerEncoder.EncodeWithoutIndexing(header.Name, header.Value, context.Buffer, ref context.Offset, ref context.FreeBytesLeftInBuffer);
                else
                    _headerEncoder.Encode(header.Name, header.Value, context.Buffer, ref context.Offset, ref context.FreeBytesLeftInBuffer);
            }


            //4 = END_HEADERS
            var flags = frame.IsEndOfStream ? 5 : 4;
            var payloadLength = context.Offset - startOffset;
            context.Buffer[headerOffset] = (byte)payloadLength;
            context.Buffer[headerOffset + 1] = (byte)(payloadLength & 0xff00);
            context.Buffer[headerOffset + 2] = (byte)(payloadLength & 0xff0000);
            context.Buffer[headerOffset + 3] = frame.FrameType;
            context.Buffer[headerOffset + 4] = (byte)flags;
            context.Buffer[headerOffset + 5] = (byte)frame.StreamIdentifier;
            context.Buffer[headerOffset + 6] = (byte)(frame.StreamIdentifier & 0xff00);
            context.Buffer[headerOffset + 7] = (byte)(frame.StreamIdentifier & 0xff0000);
            context.Buffer[headerOffset + 8] = (byte)(frame.StreamIdentifier & 0xff000000);
            await context.SendAsync();
        }

        private async Task EncodeDataFrameAsync(EncodingContext context, HttpFrame frame)
        {
            var dataFrame = (DataFrame)frame;
            var flags = dataFrame.IsEndOfStream ? 1 : 0;
            AddHeader(context, frame.FrameType, flags, (int)dataFrame.Data.Length, frame.StreamIdentifier);
            while (true)
            {
                var read = dataFrame.Data.Read(context.Buffer, context.Offset, context.FreeBytesLeftInBuffer);
                if (read == 0)
                    break;


                context.Offset += read;
                context.FreeBytesLeftInBuffer -= read;
                if (context.FreeBytesLeftInBuffer == 0)
                    await context.SendAsync();
            }
        }

        private void AddHeader(EncodingContext context, int frameType, int flags, int length, int streamIdentifier)
        {
            context.Buffer[context.Offset++] = (byte)(length & (1 << 8));
            context.Buffer[context.Offset++] = (byte)(length & (1 << 16));
            context.Buffer[context.Offset++] = (byte)(length & (1 << 24));
            context.Buffer[context.Offset++] = (byte)frameType;
            context.Buffer[context.Offset++] = (byte)flags;
            context.Buffer[context.Offset++] = (byte)(streamIdentifier & (1 << 8));
            context.Buffer[context.Offset++] = (byte)(streamIdentifier & (1 << 16));
            context.Buffer[context.Offset++] = (byte)(streamIdentifier & (1 << 24));
            context.Buffer[context.Offset++] = (byte)(streamIdentifier & (1 << 32));
            context.FreeBytesLeftInBuffer += HeaderSize;
        }

        public int MaxFrameSize { get; set; }
    }
}