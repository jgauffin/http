namespace Http2.Frames
{
    /// <summary>
    ///     Used to implement flow control.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///     </para>
    /// </remarks>
    public class WindowUpdate : HttpFrame
    {
        public WindowUpdate(int windowsSizeIncrement) : base(8)
        {
            WindowsSizeIncrement = windowsSizeIncrement;
        }

        /// <summary>
        ///     New window size.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         <see cref="HttpFrame.StreamIdentifier" /> specifies if the window size is for the stream or the connection.
        ///     </para>
        ///     <para>
        ///         sender MUST NOT allow a flow-control window to exceed 2^31-1 octets. If a sender receives a WINDOW_UPDATE that
        ///         causes a flow-control window to exceed this maximum, it MUST terminate either the stream or the connection, as
        ///         appropriate. For streams, the sender sends a RST_STREAM with an error code of FLOW_CONTROL_ERROR; for the
        ///         connection, a GOAWAY frame with an error code of FLOW_CONTROL_ERROR is sent.
        ///     </para>
        /// </remarks>
        public int WindowsSizeIncrement { get; set; }
    }
}