using System;

namespace Http2.Frames
{
    /// <summary>
    ///     HTTP frame as specified in chapter 4 of the HTTP2 specification.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Important! Frame objects can be reused after the request have been handled. Thus keeping a reference to a frame
    ///         object          results in undefined behavior. If you need to keep information, copy the information to a new
    ///         object.
    ///     </para>
    /// </remarks>
    public class HttpFrame
    {
        public HttpFrame(byte frameType)
        {
            if (frameType <= 0) throw new ArgumentOutOfRangeException(nameof(frameType));
            FrameType = frameType;
        }

        public byte FrameType { get; set; }

        /// <summary>
        ///     This frame is the last one in this stream.
        /// </summary>
        public bool IsEndOfStream { get; set; }

        /// <summary>
        ///     stream identifier (see Section 5.1.1)
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The value 0x0 is reserved for frames that are associated with the connection as a whole as opposed to an
        ///         individual stream.
        ///     </para>
        /// </remarks>
        public int StreamIdentifier { get; set; }

        /// <summary>
        ///     boolean flags specific to the frame type
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Flags are assigned semantics specific to the indicated frame type. Flags that have no defined semantics for a
        ///         particular frame type MUST be ignored and MUST be left unset (0x0) when sending
        ///     </para>
        /// </remarks>
        public int FrameFlags { get; set; }
    }
}