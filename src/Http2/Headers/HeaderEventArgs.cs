namespace Http2.Headers
{
    /// <summary>
    ///     Event arguments for <see cref="Decoder.HeaderDecoded" />.
    /// </summary>
    public class HeaderEventArgs
    {
        /// <summary>
        ///     Header name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Header value
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        ///     Field may not be indexed if <c>true</c> (contains sensitive data like authentication).
        /// </summary>
        public bool IsIndexingAllowed { get; set; }
    }
}