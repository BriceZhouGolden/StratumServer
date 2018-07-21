using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StratumServerDotNet
{
    public class StratumClient : IStratumClient
    {
        public string IP => ((IPEndPoint)_client.Client.RemoteEndPoint)?.Address.ToString();
        /// <summary>
        /// Returns -1 when port cannot be retrieved.
        /// </summary>
        public int Port => ((IPEndPoint)_client.Client.RemoteEndPoint)?.Port ?? -1;
        /// <summary>
        /// Unique Id of stratum client.
        /// </summary>
        public int Id { get; }

        public bool Connected => _client.Connected;

        /// <summary>
        /// Time that passed from last message received from the client. Allows detecting inactive clients.
        /// </summary>
        public TimeSpan TimeFromLastMsg => DateTime.UtcNow - _lastMsgTime;

        /// <summary>
        /// Event is fired when client disconnects.
        /// </summary>
        public event DisconnectedEventHandler Disconnected;

        /// <summary>
        /// Event is fired when message is received.
        /// </summary>
        public event MessageReceivedEventHandler MessageReceived;

        private const byte MSG_END_SIGN = (byte)'\n';

        private readonly TcpClient _client;
        private readonly Queue<byte> _buffer = new Queue<byte>(512);
        private readonly byte[] _bytes = new byte[256];
        private readonly int _msgSizeLimit;
        private DateTime _lastMsgTime = DateTime.UtcNow;
        private CancellationTokenSource _listenCts = new CancellationTokenSource();

        public StratumClient(TcpClient client, int id, int messageSizeLimitInBytes = 1024, TimeSpan sendTimeout = default(TimeSpan))
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client), "TCP client is null");

            if (messageSizeLimitInBytes < 1)
                throw new ArgumentOutOfRangeException(nameof(messageSizeLimitInBytes), "Message size limit must be nonzero and positive");

            _client = client;
            Id = id;
            _msgSizeLimit = messageSizeLimitInBytes;
            _client.SendTimeout = sendTimeout.Milliseconds;
        }

        /// <summary>
        /// Sends string to client.
        /// </summary>
        public async Task SendAsync(string message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message), "Cannot send null message");

            if (!_client.Connected)
                throw new InvalidOperationException("Cannot send message to stratum client. Connection has ended");

            byte[] bytes = Encoding.UTF8.GetBytes(message);
            await Task.Run(() => _client.Client.Send(bytes));
        }

        /// <summary>
        /// Disconnects with client.
        /// </summary>
        public void Disconnect()
        {
            StopListening();

            if (!_client.Connected)
                return;

            _client.Client.Shutdown(SocketShutdown.Both);
            _client.Dispose();
        }

        /// <summary>
        /// Reads message from client. Message must end with line feed (LF = '\n') character. Doesn't fire event when new message is received.
        /// </summary>
        /// <returns>Returns null if user has disconnected.</returns>
        public async Task<string> ReadAsync()
        {
            int i;
            NetworkStream stream = _client.GetStream();

            try
            {
                while ((i = await stream.ReadAsync(_bytes, 0, _bytes.Length)) != 0)
                {
                    if (IsMessageSizeExceeded(i))
                        throw new MessageSizeExceededException($"Message size limit was exceeded - message is {GetMessageSize(i)} bytes long but the limit is {_msgSizeLimit} bytes.");

                    int endSignIndex = Array.IndexOf(_bytes, MSG_END_SIGN, 0, i);
                    if (endSignIndex != -1)
                    {
                        string msg = Encoding.UTF8.GetString(JoinWithBuffer(endSignIndex));
                        AppendToBuffer(startIndex: endSignIndex + 1, endIndex: i);
                        _lastMsgTime = DateTime.UtcNow;
                        return msg;
                    }

                    AppendToBuffer(startIndex: 0, endIndex: i);
                }
            }
            catch (IOException)
            {
                // ignore that user disconnection was caused by his error
            }

            OnDisconnected(new ClientEventArgs(this));
            Disconnect();

            return null;
        }

        /// <summary>
        /// Listen for incoming messages and fires event when new message is received.
        /// </summary>
        public async Task ListenAsync()
        {
            _listenCts = new CancellationTokenSource();
            while (true)
            {
                string message = await Task.Run(ReadAsync, _listenCts.Token);
                if (_listenCts.IsCancellationRequested)
                    return;
                OnMessageReceived(new MessageEventArgs(message));
            }
        }

        /// <summary>
        /// Stops listening for incoming messages.
        /// </summary>
        public void StopListening()
        {
            _listenCts.Cancel();
        }

        private bool IsMessageSizeExceeded(int newBytesCount)
        {
            return GetMessageSize(newBytesCount) > _msgSizeLimit;
        }

        private int GetMessageSize(int newBytesCount)
        {
            return _buffer.Count + newBytesCount;
        }

        private byte[] JoinWithBuffer(int count)
        {
            byte[] array = new byte[_buffer.Count + count];
            int index = 0;

            for (int i = 0; i < _buffer.Count; i++)
                array[index++] = _buffer.Dequeue();

            for (int i = 0; i < count; i++)
                array[index++] = _bytes[i];

            return array;
        }

        private void AppendToBuffer(int startIndex, int endIndex)
        {
            for (int i = startIndex; i < endIndex; i++)
                _buffer.Enqueue(_bytes[i]);
        }

        private void OnDisconnected(ClientEventArgs e)
        {
            Disconnected?.Invoke(this, e);
        }

        private void OnMessageReceived(MessageEventArgs e)
        {
            MessageReceived?.Invoke(this, e);
        }
    }

    public delegate void DisconnectedEventHandler(object sender, ClientEventArgs e);

    public delegate void MessageReceivedEventHandler(object sender, MessageEventArgs e);
}