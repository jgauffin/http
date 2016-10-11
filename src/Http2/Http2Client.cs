using System;
using System.Threading.Tasks;
using Http2.Codec;
using Http2.Frames;

namespace Http2
{
    public class Http2Client
    {
        private ISocketChannel _socket;
        FrameEncoder _encoder;
        private EncodingContext _context;

        public Http2Client()
        {
        }

        public async Task ConnectAsync(Uri uri)
        {

            int port = uri.Port;
            if (uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                _socket = new SecureSocketChannel();
                if (port == 0)
                    port = uri.Port == 0 ? 443 : uri.Port;
            }
            else
            {
                _socket = new SocketChannel();
                if (port == 0)
                    port = uri.Port == 0 ? 80 : uri.Port;
            }


            await _socket.ConnectAsync(uri.Host, port);

            _context = new EncodingContext(_socket);
            _encoder = new FrameEncoder(_context);


            var http2Settings = new SettingsFrame();
            http2Settings.Encode(1, new EncodingContext());

            var handshake = string.Format(@"GET / HTTP/1.1
Host: {0}
Connection: Upgrade, HTTP2-Settings
Upgrade: h2c
HTTP2-Settings: {1}
     ",uri.Host, http2Settings);

        }
    }

    public class Http2Settings
    {

    }
}