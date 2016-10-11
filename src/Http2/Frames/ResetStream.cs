namespace Http2.Frames
{
    internal class ResetStream : HttpFrame
    {
        public ResetStream(int errorCode):base(3)
        {
            ErrorCode = errorCode;
        }

        public int ErrorCode { get; set; }
    }
}