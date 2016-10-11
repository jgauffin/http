namespace Http2.Frames
{
    /// <summary>
    ///     Types for <see cref="SettingsFrame" />.
    /// </summary>
    public enum SettingsParameter
    {
        /// <summary>
        ///     Allows the sender to inform the remote endpoint of the maximum size of the header compression table used to decode
        ///     header blocks, in octets.
        /// </summary>
        /// <para>
        ///     The encoder can select any size equal to or less than this value by using signaling specific to the header
        ///     compression format inside a header block (see [COMPRESSION]). The initial value is 4,096 octets.
        /// </para>
        HeaderTableSize = 0x1,


        /// <summary>
        ///     This setting can be used to disable server push (Section 8.2).
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         An endpoint MUST NOT send a PUSH_PROMISE frame if it receives this parameter set to a value of 0. An endpoint
        ///         that has both set this parameter to 0 and had it acknowledged MUST treat the receipt of a PUSH_PROMISE frame as
        ///         a connection error (Section 5.4.1) of type PROTOCOL_ERROR.
        ///     </para>
        ///     <para>
        ///         The initial value is 1, which indicates that server push is permitted. Any value other than 0 or 1 MUST be
        ///         treated as a connection error (Section 5.4.1) of type PROTOCOL_ERROR.
        ///     </para>
        /// </remarks>
        EnablePush = 0x2,


        /// <summary>
        ///     Indicates the maximum number of concurrent streams that the sender will allow.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This limit is directional: it applies to the number of streams that the sender permits the receiver to create.
        ///         Initially, there is no limit to this value. It is recommended that this value be no smaller than 100, so as to
        ///         not unnecessarily limit parallelism.
        ///     </para>
        ///     <para>
        ///         A value of 0 for _MAX_CONCURRENT_STREAMS SHOULD NOT be treated as special by endpoints. A zero value
        ///         does prevent the creation of new streams; however, this can also happen for any limit that is exhausted with
        ///         active streams. Servers SHOULD only set a zero value for short durations; if a server does not wish to accept
        ///         requests, closing the connection is more appropriate.
        ///     </para>
        /// </remarks>
        MaxConcurrentStreams = 0x3,


        /// <summary>
        ///     Indicates the sender's initial window size (in octets) for stream-level flow control. The initial value is 216-1
        ///     (65,535) octets.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This setting affects the window size of all streams (see Section 6.9.2).
        ///     </para>
        ///     <para>
        ///         Values above the maximum flow-control window size of 231-1 MUST be treated as a connection error (Section
        ///         5.4.1) of type FLOW_CONTROL_ERROR.
        ///     </para>
        /// </remarks>
        InitialWindowSize = 0x4,


        /// <summary>
        ///     Indicates the size of the largest frame payload that the sender is willing to receive, in octets.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The initial value is 214 (16,384) octets. The value advertised by an endpoint MUST be between this initial
        ///         value and the maximum allowed frame size (224-1 or 16,777,215 octets), inclusive. Values outside this range
        ///         MUST be treated as a connection error (Section 5.4.1) of type PROTOCOL_ERROR.
        ///     </para>
        /// </remarks>
        MaxFrameSize = 0x5,


        /// <summary>
        ///     This advisory setting informs a peer of the maximum size of header list that the sender is prepared to accept, in
        ///     octets.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The value is based on the uncompressed size of header fields, including the length of the name and value in
        ///         octets plus an overhead of 32 octets for each header field.
        ///     </para>
        ///     <para>
        ///         For any given request, a lower limit than what is advertised MAY be enforced. The initial value of this setting
        ///         is unlimited.
        ///     </para>
        /// </remarks>
        MaxHeaderListSize = 0x6
    }
}