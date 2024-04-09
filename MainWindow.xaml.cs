using P2PMessenger.Networking;
using P2PMessenger.Services;
using System.Reflection.Metadata;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Serialization;

namespace P2PMessenger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Alice alice;
        private Bob bob;
        private const int Port = 12345;
        private const string DEFAULT_USER = "alice";
        private string user = DEFAULT_USER;
        public MainWindow()
        {
            InitializeComponent();

            string[] args = Environment.GetCommandLineArgs();
            user = args.Length > 1 ? args[1].ToLower() : DEFAULT_USER;

            if (user == "alice")
            {
                Title = "Alice";
                alice = new Alice(Port);
                alice.MessageReceived += Alice_MessageReceived;
                alice.KeyExchangeUpdated += UpdateKeyExchangeDisplay;
                alice.Start();
            }
            else if (user == "bob")
            {
                Title = "Bob";
                bob = new Bob("127.0.0.1", Port);
                bob.MessageReceived += Bob_MessageReceived;
                bob.KeyExchangeUpdated += UpdateKeyExchangeDisplay;
                ConnectToAlice();
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string message = inputTextBox.Text;
            if (!string.IsNullOrWhiteSpace(message))
            {
                if (user == "alice")
                {
                    await alice.SendMessageAsync(message);
                }
                else if (user == "bob")
                {
                    await bob.SendMessageAsync(message);
                }
                DisplayMessage($"Me > {message}");
                inputTextBox.Clear();
            }
        }

        private void DisplayMessage(string message)
        {
            messagesTextBox.AppendText(message + "\n");
        }

        private async void ConnectToAlice()
        {
            if (bob != null)
            {
                await bob.ConnectAsync();
            }
        }
        private void UpdateKeyExchangeDisplay(string status, byte[] sharedSecret)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateKeyExchangeStatus(status);
                if (sharedSecret != null)
                {
                    UpdateSharedSecretDisplay(sharedSecret);
                }
                else
                {
                    sharedSecretText.Text = "Shared secret is null or exchange failed.";
                }
            });
        }
        public void UpdateKeyExchangeStatus(string status)
        {
            keyExchangeStatusText.Text += "\n"+status;
        }

        public void UpdateSharedSecretDisplay(byte[] sharedSecret)
        {
            sharedSecretText.Text = BitConverter.ToString(sharedSecret).Replace("-", "");
        }

        private void Alice_MessageReceived(string message)
        {
            Dispatcher.Invoke(() => DisplayMessage($"Bob > {message}"));
        }
        private void Bob_MessageReceived(string message)
        {
            Dispatcher.Invoke(() => DisplayMessage($"Alice > {message}"));
        }

    }
}