using System;
using System.Text;
using Http2.Headers.Huffman;

namespace Http2.Headers
{
    public class Decoder
    {
        private readonly DynamicTable _dynamicTable;
        private readonly HeaderEventArgs _headerEventArgs = new HeaderEventArgs();
        private readonly HuffmanCodec _huffman = new HuffmanCodec();
        private readonly Encoding _encoding = Encoding.GetEncoding("ISO-8859-1");

        public Decoder(DynamicTable dynamicTable)
        {
            if (dynamicTable == null) throw new ArgumentNullException(nameof(dynamicTable));
            _dynamicTable = dynamicTable;
        }

        public Decoder()
        {
            _dynamicTable = new DynamicTable();
        }

        public event EventHandler<HeaderEventArgs> HeaderDecoded;

        public void Decode(byte[] buffer, ref int offset, ref int count)
        {
            while (count > 0)
            {


                _headerEventArgs.IsIndexingAllowed = true;

                // 6.1 Indexed Header Field Representation
                if ((buffer[offset] & 128) == 128)
                {
                    var index = buffer[offset] & ~128;
                    if (index == 0)
                        throw new DecoderException(Http2ErrorCode.ProtocolError, "HPACK, Indexed header 0 is not allowed.");
                    ++offset;
                    --count;

                    AssignIndexedHeaderToEventArgs(index);
                }

                // 6.2.1 Literal Header Field
                else if ((buffer[offset] & 64) == 64)
                {
                    var index = buffer[offset] & ~64;
                    ++offset;
                    --count;

                    // Header name is in the static or dynamic table
                    if (index > 0)
                        AssignIndexedHeaderToEventArgs(index);
                    else
                        _headerEventArgs.Name = ReadStringLiteral(buffer, ref offset, ref count);

                    _headerEventArgs.Value = ReadStringLiteral(buffer, ref offset, ref count);

                    // A literal header field with incremental indexing representation results in appending a 
                    // header field to the decoded header list and inserting it as a new entry into the dynamic table
                    _dynamicTable.Append(_headerEventArgs.Name, _headerEventArgs.Value);
                }

                // 6.2.2 Literal Header Field without Indexing
                else if ((buffer[offset] & 240) == 0)
                {
                    var index = buffer[offset] & ~240;
                    ++offset;
                    --count;

                    // Header name is in the static or dynamic table
                    if (index > 0)
                        AssignIndexedHeaderToEventArgs(index);
                    else
                        _headerEventArgs.Name = ReadStringLiteral(buffer, ref offset, ref count);

                    _headerEventArgs.Value = ReadStringLiteral(buffer, ref offset, ref count);
                }

                // 6.2.3 Literal Header Field Never Indexed
                else if ((buffer[offset] & 240) == 16)
                {
                    var index = buffer[offset] & ~240;
                    ++offset;
                    --count;

                    // Header name is in the static or dynamic table
                    if (index > 0)
                        AssignIndexedHeaderToEventArgs(index);
                    else
                        _headerEventArgs.Name = ReadStringLiteral(buffer, ref offset, ref count);

                    _headerEventArgs.Value = ReadStringLiteral(buffer, ref offset, ref count);
                    _headerEventArgs.IsIndexingAllowed = false;
                }

                // 6.3 Dynamic Table Size Update
                else if ((buffer[offset] & 224) == 32)
                {
                    var maxSize = buffer[offset] & ~224;
                    ++offset;
                    --count;

                    if (DynamicTableSizeReceived != null)
                        DynamicTableSizeReceived(this, new DynamicTableSizeReceivedEventArgs(maxSize));
                    return;
                }

                if (HeaderDecoded != null)
                    HeaderDecoded(this, _headerEventArgs);
            }
        }

        public event EventHandler<DynamicTableSizeReceivedEventArgs> DynamicTableSizeReceived;

        private string ReadStringLiteral(byte[] buffer, ref int offset, ref int count)
        {
            var isHuffmanEncoded = (buffer[offset] & 128) == 128;
            var stringLength = buffer[offset] & ~128;
            ++offset;
            --count;
            if (count < stringLength)
                throw new DecoderException(Http2ErrorCode.CompressionError, "HPACK, Expected more bytes available.");

            if (isHuffmanEncoded)
            {
                var decoded = HuffmanCodec.Decoder.Decode(buffer, offset, stringLength);
                offset += stringLength;
                count -= stringLength;
                return _encoding.GetString(decoded);
            }

            var result = _encoding.GetString(buffer, offset, stringLength);
            offset += stringLength;
            count -= stringLength;
            return result;
        }


        private void AssignIndexedHeaderToEventArgs(int index)
        {
            if (index < StaticTable.Count)
            {
                var header = StaticTable.Get(index);
                _headerEventArgs.Name = header.Name;
                _headerEventArgs.Value = header.Value;
            }
            else
            {
                //-1 since the index is zero based.
                var header = _dynamicTable.Get(index - StaticTable.Count - 1);
                _headerEventArgs.Name = header.Name;
                _headerEventArgs.Value = header.Value;
            }
        }
    }
}