using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Http2.Tests
{
    public class IntegerCodecTests
    {
        [Fact]
        public void should_be_able_to_decode_10_using_5bit_prefix_as_in_the_specification_example()
        {
            var expected = 10;
            var buffer = new byte[10];
            var count = buffer.Length;
            var offset = 0;

            IntegerCodec.Encode(5, 10, buffer, ref offset, ref count);
            count = offset;
            offset = 0;
            var actual = IntegerCodec.Decode(5, buffer, ref offset, ref count);

            actual.Should().Be(expected);
        }
    }
}
