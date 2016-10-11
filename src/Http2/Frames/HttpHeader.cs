using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Http2.Frames
{
    public class HttpHeader
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public bool IsIndexingAllowed { get; set; }
        public bool ContainsSensitiveData { get; set; }
    }
}
