using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Http2.Headers;
using Xunit;
using Decoder = Http2.Headers.Decoder;

namespace Http2.Tests.Headers
{
    public class RequestDecodingTestsFromTheSpecification
    {
        [Fact]
        public void C21_literal_header_with_indexing()
        {
            var hex = "400a 6375 7374 6f6d 2d6b 6579 0d63 7573 746f 6d2d 6865 6164 6572";
            var buffer = HexToBytes(hex);
            int offset = 0;
            int count = buffer.Length;
            var table = new DynamicTable();

            var decoder = new Decoder(table);
            decoder.Decode(buffer, ref offset, ref count);

            table.Get(0).Name.Should().Be("custom-key");
            table.Get(0).Value.Should().Be("custom-header");
            table.Get(0).Size.Should().Be(55);
        }

        [Fact]
        public void C22_literal_header_without_indexing()
        {
            var hex = "040c 2f73 616d 706c 652f 7061 7468";
            var buffer = HexToBytes(hex);
            var offset = 0;
            var count = buffer.Length;
            var table = new DynamicTable();
            IndexedHeader header = null;

            var decoder = new Decoder(table);
            decoder.HeaderDecoded += (sender, args) => { header = new IndexedHeader(args.Name, args.Value); };
            decoder.Decode(buffer, ref offset, ref count);

            table.IsEmpty.Should().BeTrue();
            header.Name.Should().Be(":path");
            header.Value.Should().Be("/sample/path");
        }


        [Fact]
        public void C23_literal_header_never_indexed()
        {
            var hex = "1008 7061 7373 776f 7264 0673 6563 7265 74";
            var buffer = HexToBytes(hex);
            var offset = 0;
            var count = buffer.Length;
            var table = new DynamicTable();
            HeaderEventArgs e = null;

            var decoder = new Decoder(table);
            decoder.HeaderDecoded += (sender, args) => { e = args; };
            decoder.Decode(buffer, ref offset, ref count);

            table.IsEmpty.Should().BeTrue();
            e.Name.Should().Be("password");
            e.Value.Should().Be("secret");
            e.IsIndexingAllowed.Should().BeFalse();
        }

        [Fact]
        public void C24_indexed_header_field()
        {
            var hex = "82";
            var buffer = HexToBytes(hex);
            var offset = 0;
            var count = buffer.Length;
            var table = new DynamicTable();
            HeaderEventArgs e = null;

            var decoder = new Decoder(table);
            decoder.HeaderDecoded += (sender, args) => { e = args; };
            decoder.Decode(buffer, ref offset, ref count);

            table.IsEmpty.Should().BeTrue();
            e.Name.Should().Be(":method");
            e.Value.Should().Be("GET");
            e.IsIndexingAllowed.Should().BeTrue();
        }

        [Fact]
        public void C31_first_request()
        {
            var hex = "8286 8441 0f77 7777 2e65 7861 6d70 6c65 2e63 6f6d";
            var buffer = HexToBytes(hex);
            var offset = 0;
            var count = buffer.Length;
            var table = new DynamicTable();
            var headers = new List<IndexedHeader>();

            var decoder = new Decoder(table);
            decoder.HeaderDecoded += (sender, args) => { headers.Add(new IndexedHeader(args.Name, args.Value)); };
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

        [Fact]
        public void C32_second_request()
        {
            var hex = "8286 84be 5808 6e6f 2d63 6163 6865";
            var buffer = HexToBytes(hex);
            var offset = 0;
            var count = buffer.Length;
            var table = new DynamicTable();
            var headers = new List<IndexedHeader>();
            table.Append(":authority", "www.example.com");

            var decoder = new Decoder(table);
            decoder.HeaderDecoded += (sender, args) => { headers.Add(new IndexedHeader(args.Name, args.Value)); };
            decoder.Decode(buffer, ref offset, ref count);

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
            table.Get(0).Name.Should().Be("cache-control");
            table.Get(0).Value.Should().Be("no-cache");
            table.Get(1).Name.Should().Be(":authority");
            table.Get(1).Value.Should().Be("www.example.com");
        }

        [Fact]
        public void C33_third_request()
        {
            var hex = "8287 85bf 400a 6375 7374 6f6d 2d6b 6579 0c63 7573 746f 6d2d 7661 6c75 65 ";
            var buffer = HexToBytes(hex);
            var offset = 0;
            var count = buffer.Length;
            var table = new DynamicTable();
            var headers = new List<IndexedHeader>();
            table.Append(":authority", "www.example.com");
            table.Append("cache-control", "no-cache");

            var decoder = new Decoder(table);
            decoder.HeaderDecoded += (sender, args) => { headers.Add(new IndexedHeader(args.Name, args.Value)); };
            decoder.Decode(buffer, ref offset, ref count);

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
            table.Get(0).Name.Should().Be("custom-key");
            table.Get(0).Value.Should().Be("custom-value");
            table.Get(1).Name.Should().Be("cache-control");
            table.Get(1).Value.Should().Be("no-cache");
            table.Get(2).Name.Should().Be(":authority");
            table.Get(2).Value.Should().Be("www.example.com");
        }

        [Fact]
        public void C41_first_request()
        {
            var hex = "8286 8441 8cf1 e3c2 e5f2 3a6b a0ab 90f4 ff ";
            var buffer = HexToBytes(hex);
            var offset = 0;
            var count = buffer.Length;
            var table = new DynamicTable();
            var headers = new List<IndexedHeader>();

            var decoder = new Decoder(table);
            decoder.HeaderDecoded += (sender, args) => { headers.Add(new IndexedHeader(args.Name, args.Value)); };
            decoder.Decode(buffer, ref offset, ref count);

            headers[0].Name.Should().Be(":method");
            headers[0].Value.Should().Be("GET");
            headers[1].Name.Should().Be(":scheme");
            headers[1].Value.Should().Be("http");
            headers[2].Name.Should().Be(":path");
            headers[2].Value.Should().Be("/");
            headers[3].Name.Should().Be(":authority");
            headers[3].Value.Should().Be("www.example.com");
            table.Get(0).Name.Should().Be(":authority");
            table.Get(0).Value.Should().Be("www.example.com");
        }

        [Fact]
        public void C42_second_request()
        {
            var hex = "8286 84be 5886 a8eb 1064 9cbf";
            var buffer = HexToBytes(hex);
            var offset = 0;
            var count = buffer.Length;
            var table = new DynamicTable();
            var headers = new List<IndexedHeader>();
            table.Append(":authority", "www.example.com");

            var decoder = new Decoder(table);
            decoder.HeaderDecoded += (sender, args) => { headers.Add(new IndexedHeader(args.Name, args.Value)); };
            decoder.Decode(buffer, ref offset, ref count);

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
            table.Get(0).Name.Should().Be("cache-control");
            table.Get(0).Value.Should().Be("no-cache");
            table.Get(1).Name.Should().Be(":authority");
            table.Get(1).Value.Should().Be("www.example.com");
        }

        [Fact]
        public void C43_third_request()
        {
            var hex = "8287 85bf 4088 25a8 49e9 5ba9 7d7f 8925 a849 e95b b8e8 b4bf ";
            var buffer = HexToBytes(hex);
            var offset = 0;
            var count = buffer.Length;
            var table = new DynamicTable();
            var headers = new List<IndexedHeader>();
            table.Append(":authority", "www.example.com");
            table.Append("cache-control", "no-cache");

            var decoder = new Decoder(table);
            decoder.HeaderDecoded += (sender, args) => { headers.Add(new IndexedHeader(args.Name, args.Value)); };
            decoder.Decode(buffer, ref offset, ref count);

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
            table.Get(0).Name.Should().Be("custom-key");
            table.Get(0).Value.Should().Be("custom-value");
            table.Get(1).Name.Should().Be("cache-control");
            table.Get(1).Value.Should().Be("no-cache");
            table.Get(2).Name.Should().Be(":authority");
            table.Get(2).Value.Should().Be("www.example.com");
        }


        private byte[] HexToBytes(string hex)
        {
            List<byte> result = new List<byte>();
            hex = hex.Replace(" ", "");
            var index = 0;
            while (index < hex.Length)
            {
                var ch = hex.Substring(index, 2);
                var value = int.Parse(ch, System.Globalization.NumberStyles.HexNumber);
                result.Add((byte)value);
                index += 2;
            }

            return result.ToArray();
        }
    }
}
