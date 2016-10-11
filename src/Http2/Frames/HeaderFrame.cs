using System.Collections;
using System.Collections.Generic;

namespace Http2.Frames
{
    public class HeaderFrame : HttpFrame, IEnumerable<HttpHeader>
    {
        Dictionary<string, HttpHeader>  _headers  =new Dictionary<string, HttpHeader>();

        public HeaderFrame() : base(1)
        {
            
        }
        public void Add(string name, string value, bool isIndexingAllowed)
        {
            
        }


        public IEnumerator<HttpHeader> GetEnumerator()
        {
            return _headers.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count { get { return _headers.Count; } }
        
    }
}