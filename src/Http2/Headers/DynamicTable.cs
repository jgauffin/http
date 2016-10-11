using System;
using System.Collections.Generic;

namespace Http2.Headers
{
    /// <summary>
    ///     Dynamic table as described in the HPACK specification Chapter 4.
    /// </summary>
    public class DynamicTable
    {
        /// <summary>
        ///     SETTINGS_HEADER_TABLE_SIZE in rfc7540 section 6.5.2
        /// </summary>
        public const int DefaultSize = 4096;

        /// <summary>
        ///     account for an estimated overhead associated with an entry. For example, an entry structure using two 64-bit
        ///     pointers to reference the name and the value of the entry and two 64-bit integers for counting the number of
        ///     references to the name and value would have 32 octets of overhead.
        /// </summary>
        public const int HeaderPadding = 32;

        private readonly List<IndexedHeader> _headers = new List<IndexedHeader>(DefaultSize);

        private int _currentSize;
        private int _maxSize = DefaultSize;

        /// <summary>
        ///     Checks if the table is empty.
        /// </summary>
        public bool IsEmpty
        {
            get { return _headers.Count == 0; }
        }

        /// <summary>
        ///     Resize the dynamic table
        /// </summary>
        /// <param name="newSize">New dynamic table size (in octets per the specification)</param>
        /// <remarks>
        ///     <para>
        ///         Whenever the maximum size for the dynamic table is reduced, entries are evicted from the end of the dynamic
        ///         table until the size of the dynamic table is less than or equal to the maximum size.
        ///     </para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Less or equal to zero</exception>
        public void Resize(int newSize)
        {
            if (newSize <= 0) throw new ArgumentOutOfRangeException(nameof(newSize));

            while (_currentSize > newSize)
            {
                var size = _headers[_headers.Count - 1].Size;
                _headers.RemoveAt(_headers.Count - 1);
                _currentSize -= size;
            }

            _maxSize = newSize;
        }

        /// <summary>
        ///     Get entry
        /// </summary>
        /// <param name="index">0-based index, i.e. it is not taking into account the size of the static table.</param>
        /// <returns>Header</returns>
        /// <exception cref="ArgumentOutOfRangeException">index</exception>
        public IndexedHeader Get(int index)
        {
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index), index, "Less than zero");
            if (index >= _headers.Count)
                throw new ArgumentOutOfRangeException(nameof(index), index,
                    "Larger than the amount of entries in the table.");

            // HPACK 3.2: The header field is inserted at the beginning of the dynamic table
            return _headers[_headers.Count - 1 - index];
        }

        /// <summary>
        ///     Append an header to the end of the dynamic table.
        /// </summary>
        /// <param name="name">Header name</param>
        /// <param name="value">Header value</param>
        /// <remarks>
        ///     <para>
        ///         Will evict entires per HPACK specification Section 4.4 if required.
        ///     </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">name; value</exception>
        public int Append(string name, string value)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (value == null) throw new ArgumentNullException(nameof(value));

            var header = new IndexedHeader(name, value);

            /*Before a new entry is added to the dynamic table, entries are evicted from the 
            * end of the dynamic table until the size of the dynamic table is less than or 
            * equal to (maximum size - new entry size) or until the table is empty.
            */
            var newSize = _maxSize - header.Size;
            while (_currentSize > newSize)
            {
                var size = _headers[_headers.Count - 1].Size;
                _headers.RemoveAt(_headers.Count - 1);
                _currentSize -= size;
            }

            _headers.Add(header);
            _currentSize += header.Size;
            return StaticTable.Count;
        }

        public bool TryGetIndex(string name, string wantedValue, out IndexedHeader header)
        {
            //value is not interesting, get us the first possible header.
            if (wantedValue == null)
            {
                for (var i = 0; i < _headers.Count; i++)
                {
                    if (!_headers[i].Name.Equals(name))
                        continue;

                    header = _headers[i];
                    header.IndexPosition = _headers.Count - i + StaticTable.Count;
                    return true;
                }
            }


            var index = -1;
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < _headers.Count; i++)
            {
                if (!_headers[i].Name.Equals(name))
                    continue;

                if (_headers[i].Value.Equals(wantedValue))
                {
                    index = i;
                    break;
                }

                if (index == -1)
                    index = i;
            }

            if (index == -1)
            {
                header = null;
                return false;
            }

            header = _headers[index];
            header.IndexPosition = _headers.Count - index + StaticTable.Count;
            return true;
        }
    }
}