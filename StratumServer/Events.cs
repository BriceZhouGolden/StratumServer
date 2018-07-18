using System;

namespace StratumServerDotNet
{
    public class ClientEventArgs : EventArgs
    {
        public StratumClient Client { get; }

        public ClientEventArgs(StratumClient client)
        {
            Client = client;
        }
    }

    public class MessageEventArgs : EventArgs
    {
        public string Message { get; }

        public MessageEventArgs(string message)
        {
            Message = message;
        }
    }
}
