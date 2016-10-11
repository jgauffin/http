using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Http2.Tests.Headers
{
    public static class TestExtensions
    {
        public static string ToHex(this byte[] buffer, int count = -1)
        {
            if (count == -1)
                count = buffer.Length;

            var str = "";
            for (int i = 0; i < count; i++)
            {
                if (buffer[i] == 0)
                {
                    var allZeros = true;
                    for (int j = i; j < count; j++)
                    {
                        if (buffer[j] == 0)
                            continue;
                        allZeros = true;
                        break;
                    }
                    if (allZeros)
                        return str;
                }

                str += string.Format("{0} ", buffer[i].ToString("x"));
                if ((i%10) == 0)
                    str += "\r\n";
            }

            return str;
        }

    }
}
