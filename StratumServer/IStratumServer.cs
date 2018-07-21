using System.Collections.Generic;
using System.Threading.Tasks;

namespace StratumServerDotNet
{
    public interface IStratumServer
    {
        IEnumerable<IStratumClient> Clients { get; }

        event ClientConnectedEventHandler ClientConnected;

        Task<IStratumClient> AcceptClientAsync();
        Task Broadcast(string message);
        Task ListenAsync();
        void Start();
        void Stop();
        void StopListening();
    }
}