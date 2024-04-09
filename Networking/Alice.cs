using P2PMessenger.Services;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace P2PMessenger.Networking
{
    public class Alice
    {
        private TcpListener _tcpListener;
        private TcpClient _tcpClient;
        public event Action<string> MessageReceived;
        private NetworkStream _networkStream;
        private StreamWriter _writer;
        private ECDiffieHellmanCng aliceDH;
        private byte[] alicePublicKey;
        public delegate void KeyExchangeUpdateHandler(string status, byte[] sharedSecret);
        public event KeyExchangeUpdateHandler KeyExchangeUpdated;
        private byte[] sharedSecret = null;

        public Alice(int port)
        {
            _tcpListener = new TcpListener(IPAddress.Any, port);
            aliceDH = KeyExchange.GenerateECDHKey();
            alicePublicKey = KeyExchange.GetPublicKey(aliceDH);
        }

        public void Start()
        {
            _tcpListener.Start();
            Task.Run(() => AcceptClients());
        }

        private async Task AcceptClients()
        {
            while (true)
            {
                TcpClient client = await _tcpListener.AcceptTcpClientAsync();
                Task.Run(() => HandleClient(client));
            }
        }

        private async Task HandleClient(TcpClient client)
        {
            using var stream = client.GetStream();
            var reader = new StreamReader(stream);

            while (client.Connected)
            {
                if (_tcpClient is null)
                    _tcpClient = client;

                if(_networkStream is null)
                    _networkStream = _tcpClient.GetStream();
                
                if(_writer is null)
                    _writer = new StreamWriter(_networkStream) { AutoFlush = true };
                
                if (sharedSecret is null) 
                {
                    //send alice public key
                    stream.Write(alicePublicKey, 0, alicePublicKey.Length);
                    KeyExchangeUpdated?.Invoke($"Sending Alice's Public Key : {Convert.ToBase64String(alicePublicKey)}", null);

                    //receive bobs public key
                    byte[] bobPublicKey = new byte[256]; // Adjust size based on expected key size
                    stream.Read(bobPublicKey, 0, bobPublicKey.Length);
                    KeyExchangeUpdated?.Invoke($"Received Bob's Public Key : {Convert.ToBase64String(bobPublicKey)}", null);
                    //compute
                    sharedSecret = KeyExchange.ComputeSharedSecret(aliceDH, bobPublicKey);
                    KeyExchangeUpdated?.Invoke("Generated Shared Secret", sharedSecret);
                }
                

                if (stream.DataAvailable)
                {
                    var encryptedMessageString = await reader.ReadLineAsync();
                    var encryptedMessageBytes = Convert.FromBase64String(encryptedMessageString);

                    byte[] key = Encoding.UTF8.GetBytes("YourEncryptionKe");
                    byte[] iv = Encoding.UTF8.GetBytes("YourInitVectorHo");
                    string decryptedMessage = EncryptionService.DecryptStringFromBytes_Aes(encryptedMessageBytes, key, iv);

                    MessageReceived?.Invoke(decryptedMessage);
                }

                // Additional logic for Alice to send messages to Bob can be added here
            }
        }

        public async Task SendMessageAsync(string message)
        {
            if (!_tcpClient.Connected)
                throw new InvalidOperationException("Client is not connected.");

            if (_writer == null)
                throw new InvalidOperationException("No valid stream for sending messages.");

            byte[] key = Encoding.UTF8.GetBytes("YourEncryptionKe");
            byte[] iv = Encoding.UTF8.GetBytes("YourInitVectorHo");
            byte[] encryptedMessage = EncryptionService.EncryptStringToBytes_Aes(message, key, iv);
            await _writer.WriteLineAsync(Convert.ToBase64String(encryptedMessage));
        }
    }
}
