using System;

namespace Http2.Frames
{
    /// <summary>
    ///     initiate shutdown of a connection or to signal serious error conditions
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         GOAWAY allows an endpoint to gracefully stop accepting new streams while still finishing processing of
    ///         previously established streams. This enables administrative actions, like server maintenance.
    ///     </para>
    ///     <para>
    ///         There is an inherent race condition between an endpoint starting new streams and the remote sending a GOAWAY
    ///         frame. To deal with this case, the GOAWAY contains the stream identifier of the last peer-initiated stream that
    ///         was or might be processed on the sending endpoint in this connection. For instance, if the server sends a
    ///         GOAWAY frame, the identified stream is the highest-numbered stream initiated by the client.
    ///     </para>
    ///     <para>
    ///         Once sent, the sender will ignore frames sent on streams initiated by the receiver if the stream has an
    ///         identifier higher than the included last stream identifier. Receivers of a GOAWAY frame MUST NOT open
    ///         additional streams on the connection, although a new connection can be established for new streams.
    ///     </para>
    ///     <para>
    ///         If the receiver of the GOAWAY has sent data on streams with a higher stream identifier than what is indicated
    ///         in the GOAWAY frame, those streams are not or will not be processed. The receiver of the GOAWAY frame can treat
    ///         the streams as though they had never been created at all, thereby allowing those streams to be retried later on
    ///         a new connection.
    ///     </para>
    ///     <para>
    ///         Endpoints SHOULD always send a GOAWAY frame before closing a connection so that the remote peer can know
    ///         whether a stream has been partially processed or not. For example, if an HTTP client sends a POST at the same
    ///         time that a server closes a connection, the client cannot know if the server started to process that POST
    ///         request if the server does not send a GOAWAY frame to indicate what streams it might have acted on.
    ///     </para>
    ///     <para>
    ///         An endpoint might choose to close a connection without sending a GOAWAY for misbehaving peers.
    ///     </para>
    ///     <para>
    ///         A GOAWAY frame might not immediately precede closing of the connection; a receiver of a GOAWAY that has no more
    ///         use for the connection SHOULD still send a GOAWAY frame before terminating the connection.
    ///     </para>
    /// </remarks>
    public class GoAway : HttpFrame
    {
        public GoAway(int lastStreamId, int errorCode):base(7)
        {
            if (lastStreamId < 0) throw new ArgumentOutOfRangeException(nameof(lastStreamId));
            if (errorCode <= 0) throw new ArgumentOutOfRangeException(nameof(errorCode));

            LastStreamId = lastStreamId;
            ErrorCode = errorCode;
            throw new NotImplementedException();
        }

        /// <summary>
        ///     The highest-numbered stream identifier for which the sender of the GOAWAY frame might have taken some action on or
        ///     might yet take action on.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         All streams up to and including the identified stream might have been processed in some way. The last stream
        ///         identifier can be set to 0 if no streams were processed.
        ///     </para>
        /// </remarks>
        public int LastStreamId { get; set; }

        /// <summary>
        ///     Most likely one of the <see cref="Http2ErrorCode" />
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Unknown or unsupported error codes MUST NOT trigger any special behavior. These MAY be treated by an
        ///         implementation as being equivalent to INTERNAL_ERROR.
        ///     </para>
        /// </remarks>
        public int ErrorCode { get; set; }

        /// <summary>
        ///     Intended for diagnostic purposes only and carries no semantic value.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         <c>null</c> if no debug data was supplied.
        ///     </para>
        ///     <para>
        ///         Debug information could contain security- or privacy-sensitive data. Logged or otherwise persistently stored
        ///         debug data MUST have adequate safeguards to prevent unauthorized access.
        ///     </para>
        /// </remarks>
        public byte[] DebugData { get; set; }
    }
}