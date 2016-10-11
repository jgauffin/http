using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Http2.Headers;
using Xunit;
using Decoder = Http2.Headers.Decoder;
using Encoder = Http2.Headers.Encoder;

namespace Http2.Tests.Headers
{
    /// <summary>
    /// Tests to make sure that the Encoder and Decoder works together.
    /// </summary>
    public class CodecTests
    {
        /// <summary>
        /// HPACK Specification, section C.4.1
        /// </summary>
        [Fact]
        public void first_request()
        {
            var decoder = new Decoder();
            var encoder = new Encoder(8192);
            var buffer = new byte[65535];
            var offset = 0;
            var count = buffer.Length;
            var headers = new List<IndexedHeader>();
            decoder.HeaderDecoded += (sender, args) => { headers.Add(new IndexedHeader(args.Name, args.Value)); };

            encoder.Encode(":method", "GET", buffer, ref offset, ref count);
            encoder.Encode(":scheme", "http", buffer, ref offset, ref count);
            encoder.Encode(":path", "/", buffer, ref offset, ref count);
            encoder.Encode(":authority", "www.example.com", buffer, ref offset, ref count);
            count = offset;
            offset = 0;
            decoder.Decode(buffer, ref offset, ref count);

            headers[0].Name.Should().Be(":method");
            headers[0].Value.Should().Be("GET");
            headers[1].Name.Should().Be(":scheme");
            headers[1].Value.Should().Be("http");
            headers[2].Name.Should().Be(":path");
            headers[2].Value.Should().Be("/");
            headers[3].Name.Should().Be(":authority");
            headers[3].Value.Should().Be("www.example.com");
        }

        /// <summary>
        /// HPACK Specification, section C.4.2
        /// </summary>
        [Fact]
        public void second_request()
        {
            var encoderDynamicTable = new DynamicTable();
            var decoderDynamicTable = new DynamicTable();
            var decoder = new Decoder(decoderDynamicTable);
            var encoder = new Encoder(8192, encoderDynamicTable);
            var buffer = new byte[65535];
            var offset = 0;
            var count = buffer.Length;
            var headers = new List<IndexedHeader>();
            decoder.HeaderDecoded += (sender, args) => { headers.Add(new IndexedHeader(args.Name, args.Value)); };
            encoderDynamicTable.Append(":authority", "www.example.com");
            decoderDynamicTable.Append(":authority", "www.example.com");

            encoder.Encode(":method", "GET", buffer, ref offset, ref count);
            encoder.Encode(":scheme", "http", buffer, ref offset, ref count);
            encoder.Encode(":path", "/", buffer, ref offset, ref count);
            encoder.Encode(":authority", "www.example.com", buffer, ref offset, ref count);
            encoder.Encode("cache-control", "no-cache", buffer, ref offset, ref count);
            var str = buffer.ToHex();
            count = offset;
            offset = 0;
            decoder.Decode(buffer, ref offset, ref count);

            buffer[3].Should().Be(0xbe);
            headers[0].Name.Should().Be(":method");
            headers[0].Value.Should().Be("GET");
            headers[1].Name.Should().Be(":scheme");
            headers[1].Value.Should().Be("http");
            headers[2].Name.Should().Be(":path");
            headers[2].Value.Should().Be("/");
            headers[3].Name.Should().Be(":authority");
            headers[3].Value.Should().Be("www.example.com");
            headers[4].Name.Should().Be("cache-control");
            headers[4].Value.Should().Be("no-cache");
        }

        /// <summary>
        /// HPACK Specification, section C.4.3
        /// </summary>
        [Fact]
        public void third_request()
        {
            var encoderDynamicTable = new DynamicTable();
            var decoderDynamicTable = new DynamicTable();
            var decoder = new Decoder(decoderDynamicTable);
            var encoder = new Encoder(8192, encoderDynamicTable);
            var buffer = new byte[65535];
            var offset = 0;
            var count = buffer.Length;
            var headers = new List<IndexedHeader>();
            decoder.HeaderDecoded += (sender, args) => { headers.Add(new IndexedHeader(args.Name, args.Value)); };
            encoderDynamicTable.Append(":authority", "www.example.com");
            encoderDynamicTable.Append("cache-control", "no-cache");
            decoderDynamicTable.Append(":authority", "www.example.com");
            decoderDynamicTable.Append("cache-control", "no-cache");

            encoder.Encode(":method", "GET", buffer, ref offset, ref count);
            encoder.Encode(":scheme", "https", buffer, ref offset, ref count);
            encoder.Encode(":path", "/index.html", buffer, ref offset, ref count);
            encoder.Encode(":authority", "www.example.com", buffer, ref offset, ref count);
            encoder.Encode("custom-key", "custom-value", buffer, ref offset, ref count);
            var hex = buffer.ToHex();
            count = offset;
            offset = 0;
            decoder.Decode(buffer, ref offset, ref count);

            buffer[4].Should().Be(0x40, "because authority should be indexed");
            headers[0].Name.Should().Be(":method");
            headers[0].Value.Should().Be("GET");
            headers[1].Name.Should().Be(":scheme");
            headers[1].Value.Should().Be("https");
            headers[2].Name.Should().Be(":path");
            headers[2].Value.Should().Be("/index.html");
            headers[3].Name.Should().Be(":authority");
            headers[3].Value.Should().Be("www.example.com");
            headers[4].Name.Should().Be("custom-key");
            headers[4].Value.Should().Be("custom-value");
        }


    }
}
