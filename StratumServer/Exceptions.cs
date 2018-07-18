using System;

namespace StratumServerDotNet
{
    public class MessageSizeExceededException : Exception
    {
        public MessageSizeExceededException(string message) : base(message)
        {
            // ignored
        }
    }
}
