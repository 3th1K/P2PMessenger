using P2PMessenger.Services;
using System.Collections;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace P2PMessenger.Networking
{
    public class Bob
    {
        private readonly TcpClient _tcpClient;
        private readonly string _serverAddress;
        private readonly int _serverPort;
        private NetworkStream _networkStream;
        private StreamWriter _writer;
        public event Action<string> MessageReceived;
        private ECDiffieHellmanCng bobDH;
        private byte[] bobPublicKey;
        public delegate void KeyExchangeUpdateHandler(string status, byte[] sharedSecret);
        public event KeyExchangeUpdateHandler KeyExchangeUpdated;
        private byte[] sharedSecret = null;

        public Bob(string serverAddress, int serverPort)
        {
            this._tcpClient = new TcpClient();
            this._serverAddress = serverAddress;
            this._serverPort = serverPort;
            bobDH = KeyExchange.GenerateECDHKey();
            bobPublicKey = KeyExchange.GetPublicKey(bobDH);
        }

        public async Task ConnectAsync()
        {
            await _tcpClient.ConnectAsync(_serverAddress, _serverPort);
            _networkStream = _tcpClient.GetStream();
            _writer = new StreamWriter(_networkStream) { AutoFlush = true };

            if (sharedSecret is null)
            {
                // Receive Alice's public key
                byte[] alicePublicKey = new byte[256]; // Adjust size based on expected key size
                _networkStream.Read(alicePublicKey, 0, alicePublicKey.Length);
                KeyExchangeUpdated?.Invoke($"Received Alice's Public Key : {Convert.ToBase64String(alicePublicKey)}", null);

                // Send Bob's public key to Alice
                _networkStream.Write(bobPublicKey, 0, bobPublicKey.Length);
                KeyExchangeUpdated?.Invoke($"Sending Bob's Public Key : {Convert.ToBase64String(bobPublicKey)}", null);

                // Compute the shared secret
                sharedSecret = KeyExchange.ComputeSharedSecret(bobDH, alicePublicKey);
                KeyExchangeUpdated?.Invoke("Generated Shared Secret", sharedSecret);
            }

            Task.Run(() => HandleIncomingMessages());
        }

        public async Task HandleIncomingMessages() 
        {
            var reader = new StreamReader(_networkStream);
            while (_tcpClient.Connected)
            {
                if (_networkStream.DataAvailable)
                {
                    var encryptedMessageString = await reader.ReadLineAsync();
                    var encryptedMessageBytes = Convert.FromBase64String(encryptedMessageString);

                    string decryptedMessage = EncryptionService.DecryptStringFromBytes_Aes(encryptedMessageBytes, sharedSecret);

                    MessageReceived?.Invoke(decryptedMessage);
                }
            }
        }

        public async Task SendMessageAsync(string message)
        {
            if (!_tcpClient.Connected)
                throw new InvalidOperationException("Client is not connected.");

            if (_writer == null)
                throw new InvalidOperationException("No valid stream for sending messages.");

            byte[] encryptedMessage = EncryptionService.EncryptStringToBytes_Aes(message, sharedSecret);
            await _writer.WriteLineAsync(Convert.ToBase64String(encryptedMessage));
        }
        

        public void CloseConnection()
        {
            _writer?.Close();
            _networkStream?.Close();
            _tcpClient.Close();
        }
    }
}
