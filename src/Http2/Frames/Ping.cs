namespace Http2.Frames
{
    /// <summary>
    ///     A mechanism for measuring a minimal round-trip time from the sender, as well as determining whether an idle
    ///     connection is still functional.
    /// </summary>
    internal class Ping : HttpFrame
    {
        public Ping(bool isPingResponse, long opaqueData):base(6)
        {
            IsPingResponse = isPingResponse;
        }

        /// <summary>
        ///     PING frame is a PING response
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         An endpoint MUST NOT respond to PING frames containing this flag.
        ///     </para>
        /// </remarks>
        public bool IsPingResponse { get; set; }
    }
}