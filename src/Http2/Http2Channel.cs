using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Http2.Codec;
using Http2.Frames;
using Http2.Headers;

namespace Http2
{
    public class Http2Channel
    {
        private ISocketChannel _channel;
        private FrameEncoder _encoder;
        private EncodingContext _encodingContext;
        private DecodingContext _decodingContext;

        public Http2Channel()
        {
        }

        /// <summary>
        /// "PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n"
        /// </summary>
        private static readonly byte[] ClientPreface = { 80, 82, 73, 32, 42, 32, 72, 84, 84, 80, 47, 50, 46, 48, 13, 10, 13, 10, 83, 77, 13, 10, 13, 10 };

        private FrameDecoder _decoder;

        public async Task ConnectAsync(Uri uri)
        {
            if (uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                _channel = new SecureSocketChannel();
                await _channel.ConnectAsync(uri.Host, uri.Port == 0 ? 443 : uri.Port);
            }
            else
            {
                _channel = new SocketChannel();
                await _channel.ConnectAsync(uri.Host, uri.Port == 0 ? 443 : uri.Port);
            }

            _encodingContext = new EncodingContext(_channel);
            _encoder = new FrameEncoder(_encodingContext);

            Buffer.BlockCopy(ClientPreface, 0, _encodingContext.Buffer, _encodingContext.Offset, ClientPreface.Length);
            _encodingContext.Offset += ClientPreface.Length;
            var settingsFrame = new SettingsFrame();
            await _encoder.EncodeAsync(settingsFrame);
            if (_encodingContext.ContainsData)
                await _encodingContext.SendAsync();

            var ackOnOurFrame = await _decoder.DecodeAsync(_decodingContext) as SettingsFrame;
            if (ackOnOurFrame == null)
            {
                //TODO: Protocol error
            }

            var serverSettings = await _decoder.DecodeAsync(_decodingContext) as SettingsFrame;
            if (serverSettings == null)
            {
                //TODO: protocol error
            }

        }

        public async Task ActAsServerAsync(Socket clientSocket)
        {
            var channel = new SocketChannel();
            channel.Assign(clientSocket);

            _encodingContext = new EncodingContext(channel);
            _decodingContext = new DecodingContext(_channel);
            _encoder = new FrameEncoder(_encodingContext);
            _decoder = new FrameDecoder();
            await channel.ReceiveAsync(_decodingContext.Buffer, _decodingContext.Offset, _decodingContext.Capacity);
            if (_decodingContext.BytesLeftToProcess < ClientPreface.Length)
            {
                //TODO: Invalid protocol
            }

            // ReSharper disable once LoopCanBeConvertedToQuery
            for (var i = 0; i < ClientPreface.Length; i++)
            {
                if (ClientPreface[i] != _decodingContext.Buffer[_decodingContext.Offset + i])
                    throw new Http2Exception(Http2ErrorCode.ProtocolError, "ClientPreface was not valid.");
            }
            _decodingContext.Offset += ClientPreface.Length;
            _decodingContext.BytesLeftToProcess -= ClientPreface.Length;

            // didn't get the settings frame directly
            if (_decodingContext.BytesLeftToProcess == 0)
                await _decodingContext.ReceiveMoreAsync();

            var frame = await _decoder.DecodeAsync(_decodingContext) as SettingsFrame;
            if (frame == null)
                throw new Http2Exception(Http2ErrorCode.ProtocolError, "Expected SETTINGS frame after client preface.");

            //ack on client frame
            await _encoder.EncodeAsync(new SettingsFrame {IsAcknowledgment = true});

            // our own settings.
            await _encoder.EncodeAsync(new SettingsFrame());


        }
    }
}