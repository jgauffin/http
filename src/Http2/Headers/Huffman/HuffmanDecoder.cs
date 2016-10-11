/*
 * Copyright 2014 Twitter, Inc.
 * Copyright 2016 Gauffin Interactive AB (C# translation)
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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Http2.Headers.Huffman
{
    public class HuffmanDecoder
    {
        private static readonly IOException EOS_DECODED = new IOException("EOS Decoded");
        private static readonly IOException INVALID_PADDING = new IOException("Invalid Padding");

        private static Node _root;

        /**
         * Creates a new Huffman decoder with the specified Huffman coding.
         * @param codes   the Huffman codes indexed by symbol
         * @param lengths the length of each Huffman code
         */

        public HuffmanDecoder(int[] codes, byte[] lengths)
        {
            if (codes.Length != 257 || codes.Length != lengths.Length)
            {
                throw new ArgumentException("Invalid HPACK exception.");
            }
            _root = BuildTree(codes, lengths);
        }

        /**
       * Decompresses the given Huffman coded string literal.
       * @param  buffer the string literal to be decoded
       * @return the output stream for the compressed data
       * @throws IOException if an I/O error occurs. In particular,
       *         an <code>IOException</code> may be thrown if the
       *         output stream has been closed.
       */

        /// <summary>
        ///     Decompresses the given Huffman coded string literal
        /// </summary>
        /// <param name="buffer">the string literal to be decoded</param>
        /// <param name="offset">start position in the buffer</param>
        /// <param name="octetSize">Number of octets required for the huffman encoding.</param>
        /// <returns></returns>
        /// <exception cref="IOException"></exception>
        public byte[] Decode(byte[] buffer, int offset, int octetSize)
        {
            var outBuffer = new List<byte>();
            var node = _root;
            var current = 0;
            var bits = 0;

            for (var i = offset; i < offset + octetSize; i++)
            {
                var b = buffer[i] & 0xFF;
                current = (current << 8) | b;
                bits += 8;
                while (bits >= 8)
                {
                    var c = (current >> (bits - 8)) & 0xFF;
                    node = node.Children[c];
                    bits -= node.Bits;
                    if (!node.IsTerminal)
                        continue;

                    if (node.Symbol == HuffmanTable.EOS)
                    {
                        throw EOS_DECODED;
                    }
                    outBuffer.Add((byte)node.Symbol);
                    node = _root;
                }
            }

            while (bits > 0)
            {
                var c = (current << (8 - bits)) & 0xFF;
                node = node.Children[c];
                if (node.IsTerminal && node.Bits <= bits)
                {
                    bits -= node.Bits;
                    outBuffer.Add((byte)node.Symbol);
                    node = _root;
                }
                else
                {
                    break;
                }
            }

            // Section 5.2. String Literal Representation
            // Padding not corresponding to the most significant bits of the code
            // for the EOS symbol (0xFF) MUST be treated as a decoding error.
            var mask = (1 << bits) - 1;
            if ((current & mask) != mask)
            {
                throw INVALID_PADDING;
            }

            return outBuffer.ToArray();
        }

        public class Node
        {
            public readonly Node[] Children; // internal nodes have Children
            public int Bits; // number of bits matched by the node
            public int Symbol; // terminal nodes have a symbol

            /**
             * Construct an internal node
             */

            public Node()
            {
                Symbol = 0;
                Bits = 8;
                Children = new Node[256];
            }

            /**
             * Construct a terminal node
             * @param symbol the symbol the node represents
             * @param bits   the number of bits matched by this node
             */

            public Node(int symbol, int bits)
            {
                Debug.Assert(bits > 0 && bits <= 8);
                Symbol = symbol;
                Bits = bits;
                Children = null;
            }

            public bool IsTerminal
            {
                get { return Children == null; }
            }
        }

        private static Node BuildTree(int[] codes, byte[] lengths)
        {
            var root = new Node();
            for (var i = 0; i < codes.Length; i++)
            {
                Insert(root, i, codes[i], lengths[i]);
            }
            return root;
        }

        private static void Insert(Node root, int symbol, int code, byte length)
        {
            // traverse tree using the most significant bytes of code
            var current = root;
            while (length > 8)
            {
                if (current.IsTerminal)
                    throw new DecoderException(Http2ErrorCode.CompressionError,
                        "HPACK. Invalid Huffman code: prefix not unique.");

                length -= 8;
                var i = (code >> length) & 0xFF;
                if (current.Children[i] == null)
                    current.Children[i] = new Node();

                current = current.Children[i];
            }

            var terminal = new Node(symbol, length);
            var shift = 8 - length;
            var start = (code << shift) & 0xFF;
            var end = 1 << shift;
            for (var i = start; i < start + end; i++)
            {
                current.Children[i] = terminal;
            }
        }
    }
}