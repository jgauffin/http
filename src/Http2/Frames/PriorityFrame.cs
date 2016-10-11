namespace Http2.Frames
{
    /// <summary>
    ///     The sender-advised priority of a stream.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A client can assign a priority for a new stream by including prioritization information in the HEADERS frame
    ///         (RFC 7540, Section 6.2) that opens the stream. At any other time, the PRIORITY frame (RFC 7540, Section 6.3) can be used to change
    ///         the priority of a stream.
    ///     </para>
    ///     <para>
    ///         The purpose of prioritization is to allow an endpoint to express how it would prefer its peer to allocate
    ///         resources when managing concurrent streams. Most importantly, priority can be used to select streams for
    ///         transmitting frames when there is limited capacity for sending.
    ///     </para>
    ///     <para>
    ///         Streams can be prioritized by marking them as dependent on the completion of other streams (RFC 7540, Section 5.3.1).
    ///         Each dependency is assigned a relative weight, a number that is used to determine the relative proportion of
    ///         available resources that are assigned to streams dependent on the same stream.
    ///     </para>
    ///     <para>
    ///         Explicitly setting the priority for a stream is input to a prioritization process. It does not guarantee any
    ///         particular processing or transmission order for the stream relative to any other stream. An endpoint cannot
    ///         force a peer to process concurrent streams in a particular order using priority. Expressing priority is
    ///         therefore only a suggestion.
    ///     </para>
    ///     <para>
    ///         Prioritization information can be omitted from messages. Defaults are used prior to any explicit values being
    ///         provided.
    ///     </para>
    /// </remarks>
    public class PriorityFrame : HttpFrame
    {
        public PriorityFrame() : base(2)
        {
            
        }
        /// <summary>
        ///     Stream that this stream (<see cref="HttpFrame.StreamIdentifier" />) depends on.
        /// </summary>
        public int DependencyStreamId { get; set; }

        /// <summary>
        ///     Stream is exklusive
        /// </summary>
        public bool IsExclusive { get; set; }

        /// <summary>
        ///     weight between 1 and 256
        /// </summary>
        public int Weight { get; set; }
    }
}