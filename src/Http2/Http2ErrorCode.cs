namespace Http2
{
    /// <summary>
    ///     HTTP error codes as defined in the HTTP2 specification chapter 7.
    /// </summary>
    public enum Http2ErrorCode
    {
        /// <summary>
        ///     The associated condition is not a result of an error. For example, a GOAWAY might include this code to indicate
        ///     graceful shutdown of a connection.
        /// </summary>
        NoError = 0,

        /// <summary>
        ///     The endpoint detected an unspecific protocol error. This error is for use when a more specific error code is not
        ///     available.
        /// </summary>
        ProtocolError = 1,


        /// <summary>
        ///     The endpoint encountered an unexpected internal error.
        /// </summary>
        InternalError = 2,


        /// <summary>
        ///     The endpoint detected that its peer violated the flow-control protocol.
        /// </summary>
        FlowControlError = 3,


        /// <summary>
        ///     The endpoint sent a SETTINGS frame but did not receive a response in a timely manner. See Section 6.5.3 ("Settings
        ///     Synchronization").
        /// </summary>
        SettingsTimeout = 4,


        /// <summary>
        ///     The endpoint received a frame after a stream was half-closed.
        /// </summary>
        StreamClosed = 5,


        /// <summary>
        ///     The endpoint received a frame with an invalid size.
        /// </summary>
        FrameSizeError = 6,


        /// <summary>
        ///     The endpoint refused the stream prior to performing any application processing (see Section 8.1.4 for details).
        /// </summary>
        RefusedStream = 7,


        /// <summary>
        ///     Used by the endpoint to indicate that the stream is no longer needed.
        /// </summary>
        Cancel = 8,


        /// <summary>
        ///     The endpoint is unable to maintain the header compression context for the connection.
        /// </summary>
        CompressionError = 9,


        /// <summary>
        ///     The connection established in response to a CONNECT request (Section 8.3) was reset or abnormally closed.
        /// </summary>
        ConnectError = 0xa,


        /// <summary>
        ///     The endpoint detected that its peer is exhibiting a behavior that might be generating excessive load.
        /// </summary>
        EnhanceYourCalm = 0xb,


        /// <summary>
        ///     The underlying transport has properties that do not meet minimum security requirements (see Section 9.2).
        /// </summary>
        InadequateSecurity = 0xc,


        /// <summary>
        ///     The endpoint requires that HTTP/1.1 be used instead of HTTP/2.
        /// </summary>
        Http11Required = 0xd
    }
}