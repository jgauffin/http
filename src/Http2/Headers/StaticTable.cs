using System;
using System.Collections.Generic;

namespace Http2.Headers
{
    /// <summary>
    ///     Static table as defined in the HPACK specification - Appendix A.
    /// </summary>
    public class StaticTable
    {
        private static readonly IndexedHeader[] Headers =
        {
            new IndexedHeader(":authority", ""), //1
            new IndexedHeader(":method", "GET"), //2
            new IndexedHeader(":method", "POST"), //3
            new IndexedHeader(":path", "/"), //4
            new IndexedHeader(":path", "/index.html"), //5
            new IndexedHeader(":scheme", "http"), //6
            new IndexedHeader(":scheme", "https"), //7
            new IndexedHeader(":status", "200"), //8
            new IndexedHeader(":status", "204"), //9
            new IndexedHeader(":status", "206"), //10
            new IndexedHeader(":status", "304"), //11
            new IndexedHeader(":status", "400"), //12
            new IndexedHeader(":status", "404"), //13
            new IndexedHeader(":status", "500"), //14
            new IndexedHeader("accept-charset", ""), //15
            new IndexedHeader("accept-encoding", "gzip, deflate"), //16
            new IndexedHeader("accept-language", ""), //17
            new IndexedHeader("accept-ranges", ""), //18
            new IndexedHeader("accept", ""), //19
            new IndexedHeader("access-control-allow-origin", ""), //20
            new IndexedHeader("age", ""), //21
            new IndexedHeader("allow", ""), //22
            new IndexedHeader("authorization", ""), //23
            new IndexedHeader("cache-control", ""), //24
            new IndexedHeader("content-disposition", ""), //25
            new IndexedHeader("content-encoding", ""), //26
            new IndexedHeader("content-language", ""), //27
            new IndexedHeader("content-length", ""), //28
            new IndexedHeader("content-location", ""), //29
            new IndexedHeader("content-range", ""), //30
            new IndexedHeader("content-type", ""), //31
            new IndexedHeader("cookie", ""), //32
            new IndexedHeader("date", ""), //33
            new IndexedHeader("etag", ""), //34
            new IndexedHeader("expect", ""), //35
            new IndexedHeader("expires", ""), //36
            new IndexedHeader("from", ""), //37
            new IndexedHeader("host", ""), //38
            new IndexedHeader("if-match", ""), //39
            new IndexedHeader("if-modified-since", ""), //40
            new IndexedHeader("if-none-match", ""), //41
            new IndexedHeader("if-range", ""), //42
            new IndexedHeader("if-unmodified-since", ""), //43
            new IndexedHeader("last-modified", ""), //44
            new IndexedHeader("link", ""), //45
            new IndexedHeader("location", ""), //46
            new IndexedHeader("max-forwards", ""), //47
            new IndexedHeader("proxy-authenticate", ""), //48
            new IndexedHeader("proxy-authorization", ""), //49
            new IndexedHeader("range", ""), //50
            new IndexedHeader("referer", ""), //51
            new IndexedHeader("refresh", ""), //52
            new IndexedHeader("retry-after", ""), //53
            new IndexedHeader("server", ""), //54
            new IndexedHeader("set-cookie", ""), //55
            new IndexedHeader("strict-transport-security", ""), //56
            new IndexedHeader("transfer-encoding", ""), //57
            new IndexedHeader("user-agent", ""), //58
            new IndexedHeader("vary", ""), //59
            new IndexedHeader("via", ""), //60
            new IndexedHeader("www-authenticate", "") //61
        };
        static Dictionary<string, int> _nameIndex = new Dictionary<string, int>();

        static StaticTable()
        {
            for (int i = 0; i < Headers.Length; i++)
            {
                _nameIndex[Headers[i].Name] = i;
                Headers[i].IndexPosition = i + 1; //one based index.
            }
        }

        /// <summary>
        ///     Number of index headers.
        /// </summary>
        public static int Count
        {
            get { return Headers.Length; }
        }

        /// <summary>
        ///     Get an entry from the static table
        /// </summary>
        /// <param name="index">One based index.</param>
        /// <returns>Entries</returns>
        public static IndexedHeader Get(int index)
        {
            if (index < 1)
                throw new ArgumentOutOfRangeException("index", index, "Index is less than 1.");
            if (index >= Count)
                throw new ArgumentOutOfRangeException("index", index, "Index is not in the static table.");

            return Headers[index - 1];
        }

        public static bool TryGetIndex(string name, string wantedValue, out IndexedHeader header)
        {
            if (wantedValue == null)
            {
                int index;
                if (_nameIndex.TryGetValue(name, out index))
                {
                    header= Headers[index];
                    return true;
                }

                header = null;
                return false;
            }

            var firstIndex = -1;

            // ReSharper disable once ForCanBeConvertedToForeach
            for (int i = 0; i < Headers.Length; i++)
            {
                if (!Headers[i].Name.Equals(name))
                    continue;

                if (Headers[i].Value.Equals(wantedValue))
                {
                    header = Headers[i];
                    return true;
                }
                if (firstIndex == -1)
                    firstIndex = i;
            }

            if (firstIndex != -1)
            {
                header = Headers[firstIndex];
                return true;
            }

            header = null;
            return false;
        }
    }
}