using System;

namespace Http2.Headers
{
    /// <summary>
    ///     Event arguments for <see cref="Decoder.DynamicTableSizeReceived" />.
    /// </summary>
    public class DynamicTableSizeReceivedEventArgs : EventArgs
    {
        /// <summary>
        ///     creates a new instance of <see cref="DynamicTableSizeReceivedEventArgs" />.
        /// </summary>
        /// <param name="maxSize">
        ///     The new maximum size MUST be lower than or equal to the limit determined by the protocol using
        ///     HPACK. A value that exceeds this limit MUST be treated as a decoding error
        /// </param>
        public DynamicTableSizeReceivedEventArgs(int maxSize)
        {
            MaxSize = maxSize;
        }

        /// <summary>
        ///     The new maximum size MUST be lower than or equal to the limit determined by the protocol using HPACK. A value that
        ///     exceeds this limit MUST be treated as a decoding error
        /// </summary>
        public int MaxSize { get; private set; }
    }
}