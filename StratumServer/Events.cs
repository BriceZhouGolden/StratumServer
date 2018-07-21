using System;

namespace StratumServerDotNet
{
    public class ClientEventArgs : EventArgs
    {
        public IStratumClient Client { get; }

        public ClientEventArgs(IStratumClient client)
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
