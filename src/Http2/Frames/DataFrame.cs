using System;
using System.IO;

namespace Http2.Frames
{
    public class DataFrame : HttpFrame, IDisposable
    {
        private readonly Action<DataFrame> _reuseAction;

        public DataFrame():base(0)
        {
        }

        public DataFrame(Action<DataFrame> reuseAction) : base(0)
        {
            _reuseAction = reuseAction;
        }

        public Stream Data { get; set; }
        public int ContentLength { get; set; }

        public void Dispose()
        {
            if (_reuseAction != null)
            {
                _reuseAction(this);
            }
            else
            {
                if (Data != null)
                {
                    Data.Dispose();
                    Data = null;
                }
            }
        }
    }
}