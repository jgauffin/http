namespace Http2
{
    public static class IntegerCodec
    {
        public static void Encode(int bitPrefix, int value, byte[] buffer, ref int offset, ref int count)
        {
            /*  if I < 2^N - 1, encode I on N bits
                else
                    encode (2^N - 1) on N bits
                    I = I - (2^N - 1)
                    while I >= 128
                         encode (I % 128 + 128) on 8 bits
                         I = I / 128
                    encode I on 8 bits
            */
            var startOffset = offset;
            var bitPrefixValue = (1 << bitPrefix) - 1;
            if (value < bitPrefixValue)
            {
                buffer[offset++] = (byte) value;
                count--;
                return;
            }

            buffer[offset++] = (byte)bitPrefixValue;

            value = value - bitPrefixValue;
            while (value>128)
            {
                buffer[offset++] = (byte)(value % 128 + 128);
                value = value/128;
            }

            buffer[offset++] = (byte)value;
            count -= offset - startOffset;
        }

        public static int Decode(int bitPrefix, byte[] buffer, ref int offset, ref int count)
        {
            /*  decode I from the next N bits
                if I < 2^N - 1, return I
                else
                    M = 0
                    repeat
                        B = next octet
                        I = I + (B & 127) * 2^M
                        M = M + 7
                    while B & 128 == 128
                    return I
            */

            var bitPrefixValue = (1 << bitPrefix) - 1;
            if (buffer[offset] < bitPrefixValue)
            {
                count--;
                return buffer[offset++] & bitPrefixValue;
            }

            var value = bitPrefixValue;
            ++offset;
            var m = 0;
            do
            {
                value += (buffer[offset] & 127)*(1 << m);
                m += 7;
            } while ((buffer[offset++] & 128) == 128);

            return value;
        }
    }
}