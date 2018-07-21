using System;
using System.Threading.Tasks;

namespace StratumServerDotNet
{
    public interface IStratumClient
    {
        bool Connected { get; }
        int Id { get; }
        string IP { get; }
        int Port { get; }
        TimeSpan TimeFromLastMsg { get; }

        event DisconnectedEventHandler Disconnected;
        event MessageReceivedEventHandler MessageReceived;

        void Disconnect();
        Task ListenAsync();
        Task<string> ReadAsync();
        Task SendAsync(string message);
        void StopListening();
    }
}