using P2PMessenger.Security;
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
        private NetworkStream? _networkStream;
        private StreamWriter? _writer;
        public event Action<string>? MessageReceived;
        public event Action<string>? MessageSent;
        private ECDiffieHellmanCng bobDH;
        private byte[] bobPublicKey;
        public delegate void KeyExchangeUpdateHandler(string status, byte[]? sharedSecret, bool clean = false);
        public event KeyExchangeUpdateHandler? KeyExchangeUpdated;
        private byte[]? sharedSecret;
        private DateTime lastKeyUpdateTime;
        private const int KeyUpdateInterval = 1;

        public Bob(string serverAddress, int serverPort)
        {
            _tcpClient = new TcpClient();
            _serverAddress = serverAddress;
            _serverPort = serverPort;
            bobDH = SharedKeyUtility.GenerateEcdhKey();
            bobPublicKey = SharedKeyUtility.GetPublicKey(bobDH);
        }

        public async Task ConnectAsync()
        {
            await _tcpClient.ConnectAsync(_serverAddress, _serverPort);
            _networkStream = _tcpClient.GetStream();
            _writer = new StreamWriter(_networkStream) { AutoFlush = true };

            if (sharedSecret is null)
            {
                ExchangePublicKeys(_networkStream);
                lastKeyUpdateTime = DateTime.Now;
            }

            _ = Task.Run(() => HandleIncomingMessages());
        }

        public void CheckAndRenewKey()
        {
            if ((DateTime.Now - lastKeyUpdateTime).TotalMinutes > KeyUpdateInterval)
            {
                RenewKey();
            }
        }

        private void RenewKey()
        {
            // Regenerate DH parameters and perform key exchange as done initially
            bobDH = SharedKeyUtility.GenerateEcdhKey();

            // Send new public key to Bob and receive Bob's new public key, compute new shared secret
            bobPublicKey = SharedKeyUtility.GetPublicKey(bobDH);
            
            if(_networkStream is not null)
                ExchangePublicKeys(_networkStream);

            lastKeyUpdateTime = DateTime.Now;
            // Update encryption/decryption mechanisms with new shared secret
        }

        public async Task HandleIncomingMessages() 
        {
            if (_networkStream is not null)
            {
                var reader = new StreamReader(_networkStream);
                while (_tcpClient.Connected)
                {
                    CheckAndRenewKey();
                    if (_networkStream.DataAvailable)
                    {
                        var encryptedMessageString = await reader.ReadLineAsync();
                        if (encryptedMessageString is not null) 
                        {
                            var encryptedMessageBytes = Convert.FromBase64String(encryptedMessageString);
                            if (encryptedMessageBytes is not null && sharedSecret is not null) 
                            {
                                string decryptedMessage = EncryptionUtility.Decrypt(encryptedMessageBytes, sharedSecret);
                                MessageReceived?.Invoke($"\t[ CYPHERTEXT ] {encryptedMessageString}\n\t[ PLAINTEXT ] {decryptedMessage}");
                            }
                            MessageReceived?.Invoke($"Failure");
                        }

                    }
                }
            }
        }

        public async Task SendMessageAsync(string message)
        {
            if (!_tcpClient.Connected)
                throw new InvalidOperationException("Client is not connected.");

            if (_writer == null)
                throw new InvalidOperationException("No valid stream for sending messages.");
            if (sharedSecret is not null)
            {
                byte[] encryptedMessageBytes = EncryptionUtility.Encrypt(message, sharedSecret);
                string encryptedMessageString = Convert.ToBase64String(encryptedMessageBytes);
                await _writer.WriteLineAsync(encryptedMessageString);

                MessageSent?.Invoke($"\t[ CYPHERTEXT ] {encryptedMessageString}\n\t[ PLAINTEXT ] {message}");
            }
        }

        private void ExchangePublicKeys(NetworkStream stream)
        {
            // Receive Alice's public key
            byte[] alicePublicKey = new byte[256]; // Adjust size based on expected key size
            stream.Read(alicePublicKey, 0, alicePublicKey.Length);
            KeyExchangeUpdated?.Invoke($"Received Alice's Public Key : {Convert.ToBase64String(alicePublicKey)}", null, true);

            // Send Bob's public key to Alice
            stream.Write(bobPublicKey, 0, bobPublicKey.Length);
            KeyExchangeUpdated?.Invoke($"Sending Bob's Public Key : {Convert.ToBase64String(bobPublicKey)}", null);

            // Compute the shared secret
            sharedSecret = SharedKeyUtility.GenerateSharedSecret(bobDH, alicePublicKey);
            KeyExchangeUpdated?.Invoke("Generated Shared Secret", sharedSecret);
        }


        public void CloseConnection()
        {
            _writer?.Close();
            _networkStream?.Close();
            _tcpClient.Close();
        }
    }
}
