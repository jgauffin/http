using System;

namespace Http2.Headers
{
    /// <summary>
    ///     A header either in the static or dynamic table.
    /// </summary>
    public class IndexedHeader
    {
        /// <summary>
        ///     Creates a new instance of <see cref="IndexedHeader" />.
        /// </summary>
        /// <param name="name">header name</param>
        /// <param name="value">header value</param>
        public IndexedHeader(string name, string value)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (value == null) throw new ArgumentNullException(nameof(value));

            Name = name;
            Value = value;
            Size = name.Length + value.Length + DynamicTable.HeaderPadding;
        }

        /// <summary>
        /// Index in the Index Address Space (2.3.3 in the HPACK specification)
        /// </summary>
        public int IndexPosition { get; set; }

        /// <summary>
        ///     Name
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        ///     Value
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        ///     Total size of the entry, including HPACK padding.
        /// </summary>
        public int Size { get; private set; }
    }
}