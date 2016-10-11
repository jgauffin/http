namespace Http2.Headers.Huffman
{
    public class HuffmanCodec
    {
        public static HuffmanDecoder Decoder = new HuffmanDecoder(HuffmanTable.Codes, HuffmanTable.CodeLengths);
    }
}