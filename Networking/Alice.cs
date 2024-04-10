using P2PMessenger.Security;
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
        public event Action<string> MessageSent;
        private NetworkStream _networkStream;
        private StreamWriter _writer;
        private ECDiffieHellmanCng aliceDH;
        private byte[] alicePublicKey;
        public delegate void KeyExchangeUpdateHandler(string status, byte[] sharedSecret, bool clean = false);
        public event KeyExchangeUpdateHandler KeyExchangeUpdated;
        private byte[] sharedSecret = null;
        private DateTime lastKeyUpdateTime;
        private const int KeyUpdateInterval = 1;

        public Alice(int port)
        {
            _tcpListener = new TcpListener(IPAddress.Any, port);
            aliceDH = SharedKeyUtility.GenerateEcdhKey();
            alicePublicKey = SharedKeyUtility.GetPublicKey(aliceDH);
        }

        public void Start()
        {
            _tcpListener.Start();
            Task.Run(() => AcceptClients());
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
            aliceDH = SharedKeyUtility.GenerateEcdhKey();
            // Send new public key to Bob and receive Bob's new public key, compute new shared secret
            alicePublicKey = SharedKeyUtility.GetPublicKey(aliceDH);
            // Assume method for exchanging DH public keys and computing shared secret
            //using var stream = _tcpClient.GetStream();
            ExchangePublicKeys(_networkStream);

            lastKeyUpdateTime = DateTime.Now;
            // Update encryption/decryption mechanisms with new shared secret
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
                    ExchangePublicKeys(stream);
                    lastKeyUpdateTime = DateTime.Now;
                }
                CheckAndRenewKey();

                if (stream.DataAvailable)
                {
                    var encryptedMessageString = await reader.ReadLineAsync();
                    var encryptedMessageBytes = Convert.FromBase64String(encryptedMessageString);
                    try
                    {
                        string decryptedMessage = EncryptionUtility.Decrypt(encryptedMessageBytes, sharedSecret);
                        MessageReceived?.Invoke($"\t[ CYPHERTEXT ] {encryptedMessageString}\n\t[ PLAINTEXT ] {decryptedMessage}");
                    }
                    catch (Exception ex) { 
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

            byte[] encryptedMessageBytes = EncryptionUtility.Encrypt(message, sharedSecret);
            string encryptedMessageString = Convert.ToBase64String(encryptedMessageBytes);
            await _writer.WriteLineAsync(encryptedMessageString);
            
            MessageSent?.Invoke($"\t[ CYPHERTEXT ] {encryptedMessageString}\n\t[ PLAINTEXT ] {message}");

        }

        private void ExchangePublicKeys(NetworkStream stream) 
        {
            //send alice public key
            stream.Write(alicePublicKey, 0, alicePublicKey.Length);
            KeyExchangeUpdated?.Invoke($"Sending Alice's Public Key : {Convert.ToBase64String(alicePublicKey)}", null, true);

            //receive bobs public key
            byte[] bobPublicKey = new byte[256];
            stream.Read(bobPublicKey, 0, bobPublicKey.Length);
            KeyExchangeUpdated?.Invoke($"Received Bob's Public Key : {Convert.ToBase64String(bobPublicKey)}", null);
            //compute
            sharedSecret = SharedKeyUtility.GenerateSharedSecret(aliceDH, bobPublicKey);
            KeyExchangeUpdated?.Invoke("Generated Shared Secret", sharedSecret);
        }
    }
}
