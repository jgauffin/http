/*
 * Copyright 2014 Twitter, Inc.
 * Copyright 2014 Gauffin Interactive AB (C# version)
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Http2.Headers.Huffman
{
    public sealed class HuffmanEncoder
    {
        private readonly int[] _codes;
        private readonly byte[] _lengths;

        /**
         * Creates a new Huffman encoder with the specified Huffman coding.
         * @param codes   the Huffman codes indexed by symbol
         * @param lengths the length of each Huffman code
         */

        public HuffmanEncoder(int[] codes, byte[] lengths)
        {
            if (codes == null) throw new ArgumentNullException(nameof(codes));
            if (lengths == null) throw new ArgumentNullException(nameof(lengths));

            _codes = codes;
            _lengths = lengths;
        }

        /// <summary>
        ///     Compresses the input string literal using the Huffman coding
        /// </summary>
        /// <param name="sourceBuffer">the string literal to be Huffman encoded</param>
        /// <param name="sourceOffset">the start sourceOffset in the sourceBuffer</param>
        /// <param name="octetCount">the number of bytes to encode</param>
        public int Encode(byte[] sourceBuffer, int sourceOffset, int octetCount, byte[] destinationBuffer,
            ref int destinationOffset, ref int bytesAvailableInDestinationBuffer)
        {
            if (sourceBuffer == null)
                throw new ArgumentNullException("sourceBuffer");
            if (sourceOffset < 0 || octetCount < 0 || sourceOffset + octetCount < 0 ||
                sourceOffset > sourceBuffer.Length || sourceOffset + octetCount > sourceBuffer.Length)
                throw new ArgumentOutOfRangeException();
            if (octetCount == 0)
                return 0;

            var startOffset = destinationOffset;
            long current = 0;
            var n = 0;

            for (var i = 0; i < octetCount; i++)
            {
                var b = sourceBuffer[sourceOffset + i] & 0xFF;
                var code = (uint) _codes[b];
                int nbits = _lengths[b];

                current <<= nbits;
                current |= code;
                n += nbits;

                while (n >= 8)
                {
                    n -= 8;
                    destinationBuffer[destinationOffset++] = (byte) (current >> n);
                }
            }

            if (n > 0)
            {
                current <<= 8 - n;
                current |= (uint) (0xFF >> n); // this should be EOS symbol
                destinationBuffer[destinationOffset++] = (byte) current;
            }
            bytesAvailableInDestinationBuffer -= destinationOffset - startOffset;
            return destinationOffset - startOffset;
        }


        public int GetEncodedLength(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            long len = 0;

            // ReSharper disable once ForCanBeConvertedToForeach
            // ReSharper disable once LoopCanBeConvertedToQuery
            for (var i = 0; i < data.Length; i++)
            {
                len += _lengths[data[i] & 0xFF];
            }
            return (int)((len + 7) >> 3);
        }
    }
}