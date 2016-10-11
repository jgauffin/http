using System;
using System.Text;
using Http2.Headers.Huffman;

namespace Http2.Headers
{
    public class Encoder
    {
        private readonly DynamicTable _dynamicTable;
        private readonly Encoding _encoding = Encoding.GetEncoding("ISO-8859-1");
        private readonly int _maxDynamicTableSize;
        private readonly HuffmanEncoder _encoder = new HuffmanEncoder(HuffmanTable.Codes, HuffmanTable.CodeLengths);


        public Encoder(int maxDynamicTableSize)
            :this(maxDynamicTableSize, new DynamicTable())
        {
        }

        public Encoder(int maxDynamicTableSize, DynamicTable dynamicTable)
        {
            _maxDynamicTableSize = maxDynamicTableSize;
            _dynamicTable = dynamicTable;
        }

        public void Encode(string name, string value, byte[] buffer, ref int offset, ref int bytesAvailableInBuffer)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            var header = GetIndexedHeader(name, value);
            if (header != null && header.Name.Length >= _maxDynamicTableSize)
                header = null;

            if (header != null && header.Value.Equals(value))
            {
                var flagOffset = offset;
                IntegerCodec.Encode(7, header.IndexPosition, buffer, ref offset, ref bytesAvailableInBuffer);
                buffer[flagOffset] += 128;
                return;
            }

            //Literal Header Field with Incremental Indexing — New Name
            if (header == null)
            {
                buffer[offset++] = 64;
                bytesAvailableInBuffer -= 1;
                _dynamicTable.Append(name, value);
                EncodeLiteral(name, buffer, ref offset, ref bytesAvailableInBuffer);
            }
            else
            {
                var flagOffset = offset;
                IntegerCodec.Encode(6, header.IndexPosition, buffer, ref offset, ref bytesAvailableInBuffer);
                buffer[flagOffset] += 64;

            }

            EncodeLiteral(value, buffer, ref offset, ref bytesAvailableInBuffer);
        }

        public void EncodeWithoutIndexing(string name, string value, byte[] buffer, ref int offset,
            ref int bytesAvailableInBuffer)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            var header = GetIndexedHeader(name, value);
            if (header != null && header.Name.Length >= _maxDynamicTableSize)
                header = null;

            //Literal Header Field without Indexing — Indexed Name
            if (header != null)
            {
                IntegerCodec.Encode(4, header.IndexPosition, buffer, ref offset, ref bytesAvailableInBuffer);
            }

            //Literal Header Field without Indexing — New Name
            else
            {
                buffer[offset++] = 0;
                bytesAvailableInBuffer--;
                EncodeLiteral(name, buffer, ref offset, ref bytesAvailableInBuffer);
            }

            EncodeLiteral(value, buffer, ref offset, ref bytesAvailableInBuffer);
        }

        public void EncodeSensitive(string name, string value, byte[] buffer, ref int offset,
            ref int bytesAvailableInBuffer)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            var header = GetIndexedHeader(name, value);
            if (header != null && header.Name.Length >= _maxDynamicTableSize)
                header = null;

            //Literal Header Field Never Indexed — Indexed Name
            if (header != null)
            {
                var indexOffset = offset;
                IntegerCodec.Encode(4, header.IndexPosition, buffer, ref offset, ref bytesAvailableInBuffer);
                buffer[indexOffset] += 8;
            }

            //Literal Header Field Never Indexed — New Name
            else
            {
                buffer[offset++] = 8;
                bytesAvailableInBuffer--;
                EncodeLiteral(name, buffer, ref offset, ref bytesAvailableInBuffer);
            }
            
            EncodeLiteral(value, buffer, ref offset, ref bytesAvailableInBuffer);
        }

        private void EncodeLiteral(string literalValue, byte[] buffer, ref int offset, ref int bytesAvailableInBuffer)
        {
            var buf = _encoding.GetBytes(literalValue);
            var octets = _encoder.GetEncodedLength(buf);
            var huffmanOffset = offset;
            IntegerCodec.Encode(7, octets, buffer, ref offset, ref bytesAvailableInBuffer);
            buffer[huffmanOffset] += 128;
            _encoder.Encode(buf, 0, buf.Length, buffer, ref offset, ref bytesAvailableInBuffer);
        }

        private IndexedHeader GetIndexedHeader(string name, string wantedValue)
        {
            IndexedHeader header;

            // we need to look in the dynamic table first
            // as we can have the same header as in the static table, but with a defined value.
            if (_dynamicTable.TryGetIndex(name, wantedValue, out header))
                return header;

            if (StaticTable.TryGetIndex(name, wantedValue, out header))
                return header;

            return null;
        }
    }
}