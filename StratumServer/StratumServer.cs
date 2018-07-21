using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace StratumServerDotNet
{
    public class StratumServer : IStratumServer
    {
        /// <summary>
        /// Collection of active (connected) clients.
        /// </summary>
        public IEnumerable<IStratumClient> Clients => _clients.Values;

        /// <summary>
        /// Fired when new client is connected.
        /// </summary>
        public event ClientConnectedEventHandler ClientConnected;

        private TcpListener _listener;
        private readonly IPAddress _address;
        private readonly int _port;
        private readonly Dictionary<int, IStratumClient> _clients = new Dictionary<int, IStratumClient>();
        private int _nextClientId;
        private readonly int _msgSizeLimit;
        private readonly TimeSpan _clientSendTimeout;
        private CancellationTokenSource _listenCts = new CancellationTokenSource();

        public StratumServer(IPAddress address, int port, int messageSizeLimitInBytes = 1024, TimeSpan clientSendTimeout = default(TimeSpan))
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address), "IP address cannot be null");

            if (port < 0 || port > UInt16.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(address), $"Port number must be between 0 and {UInt16.MaxValue} but is {port}");

            if (messageSizeLimitInBytes < 1)
                throw new ArgumentOutOfRangeException(nameof(messageSizeLimitInBytes), "Message size limit must be nonzero and positive");

            _address = address;
            _port = port;
            _msgSizeLimit = messageSizeLimitInBytes;
            _clientSendTimeout = clientSendTimeout;
        }

        /// <summary>
        /// Starts listening for incoming connection requests.
        /// </summary>
        public void Start()
        {
            if (_listener != null)
                throw new InvalidOperationException("Stratum server is already running.");

            _listener = new TcpListener(_address, _port);
            try
            {
                _listener.Start();
            }
            catch
            {
                _listener = null;
                throw;
            }
        }

        /// <summary>
        /// Accepts a pending connection request. Server must be started before accepting stratum clients. Doesn't fire new client connected event.
        /// </summary>
        public async Task<IStratumClient> AcceptClientAsync()
        {
            if (_listener == null)
                throw new InvalidOperationException("Stratum server is not started.");

            TcpClient tcpClient = await _listener.AcceptTcpClientAsync();
            
            var client = new StratumClient(tcpClient, _nextClientId, _msgSizeLimit, _clientSendTimeout);
            client.Disconnected += OnClientDisconnected;
            _clients.Add(_nextClientId, client);
            _nextClientId++;

            return client;
        }

        /// <summary>
        /// Listen for incoming connection requests and fires event when new client is connected.
        /// </summary>
        public async Task ListenAsync()
        {
            _listenCts = new CancellationTokenSource();
            while (true)
            {
                IStratumClient client = await AcceptClientAsync();
                if (_listenCts.IsCancellationRequested)
                    return;
                OnClientConnected(new ClientEventArgs(client));
            }
        }

        /// <summary>
        /// Stops listening for incoming client connections.
        /// </summary>
        public void StopListening()
        {
            _listenCts.Cancel();
        }

        /// <summary>
        /// Sends message to all active (connected) clients.
        /// </summary>
        public async Task Broadcast(string message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message), "Cannot send null message");

            foreach (var client in Clients)
                await client.SendAsync(message);
        }

        /// <summary>
        /// Stops the server and closes connection with all clients.
        /// </summary>
        public void Stop()
        {
            StopListening();

            if (_listener == null)
                return;

            _listener.Stop();
            _listener = null;
            DisconnectAllClients();
            _clients.Clear();
        }

        private void DisconnectAllClients()
        {
            foreach (var client in Clients)
            {
                client.Disconnected -= OnClientDisconnected;
                client.Disconnect();
            }
        }

        private void OnClientConnected(ClientEventArgs e)
        {
            ClientConnected?.Invoke(this, e);
        }

        private void OnClientDisconnected(object sender, ClientEventArgs e)
        {
            _clients.Remove(e.Client.Id);
        }
    }

    public delegate void ClientConnectedEventHandler(object sender, ClientEventArgs e);
}
