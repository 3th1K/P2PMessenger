using P2PMessenger.Networking;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace P2PMessenger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Alice? alice;
        private Bob? bob;
        private const int Port = 9923;
        private const string DEFAULT_USER = "alice";
        private const string DEFAULT_USER2 = "bob";
        private string user = DEFAULT_USER;
        private string user2 = DEFAULT_USER2;
        public MainWindow()
        {
            InitializeComponent();

            string[] args = Environment.GetCommandLineArgs();
            user = args.Length > 1 ? args[1].ToLower() : DEFAULT_USER;

            if (user == "alice")
            {
                InitAlice();
                user2 = "bob";
            }
            else if (user == "bob")
            {
                InitBob();
                user2 = "alice";
            }
            else
            {
                MessageBox.Show($"Unable to fetch peer \"{user}\". Please choose Alice/Bob", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        private void InitAlice()
        {
            Title += " - Alice";
            alice = new Alice(Port);
            alice.MessageSent += OnSentMessage;
            alice.MessageReceived += OnMessageReceivedAlice;
            alice.KeyExchangeUpdated += OnKeyExchangeUpdate;
            alice.StartAlice();
        }

        private async void InitBob()
        {
            Title += " - Bob";
            bob = new Bob("127.0.0.1", Port);
            bob.MessageSent += OnSentMessage;
            bob.MessageReceived += OnMessageReceivedBob;
            bob.KeyExchangeUpdated += OnKeyExchangeUpdate;
            await bob.ConnectAsync();

        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string message = inputTextBox.Text;
            if (!string.IsNullOrWhiteSpace(message))
            {
                if (user == "alice" && alice is not null)
                {
                    await alice.SendMessageAsync(message);
                }
                else if (user == "bob" && bob is not null)
                {
                    await bob.SendMessageAsync(message);
                }
                inputTextBox.Clear();
            }
        }

        private void DisplayMessage(string message, bool isSent)
        {
            Dispatcher.Invoke(() =>
            {
                // Determine the sender name and set appropriate colors
                string senderName = isSent ? "ME" : user2.ToUpper(); // Assuming 'user' holds the name of the other participant
                SolidColorBrush messageColor = isSent ? new SolidColorBrush(Colors.Blue) : new SolidColorBrush(Colors.Green);

                // Create a new paragraph for the sender's name
                Paragraph nameParagraph = new Paragraph(new Run(senderName)
                {
                    Foreground = new SolidColorBrush(Colors.DarkGray),
                    FontWeight = FontWeights.Bold
                })
                {
                    TextAlignment = isSent ? TextAlignment.Right : TextAlignment.Left
                };

                // Create a new paragraph for the message text
                Paragraph messageParagraph = new Paragraph(new Run(message)
                {
                    Foreground = messageColor
                })
                {
                    TextAlignment = isSent ? TextAlignment.Right : TextAlignment.Left
                };

                // Optional: Add a timestamp for each message
                string timestamp = DateTime.Now.ToString("hh:mm:ss tt");
                messageParagraph.Inlines.Add(new Run($"\n{timestamp}")
                {
                    Foreground = new SolidColorBrush(Colors.Gray),
                    FontSize = 10,
                    FontWeight = FontWeights.Normal
                });

                // Add the paragraphs to the RichTextBox
                messagesTextBox.Document.Blocks.Add(nameParagraph);
                messagesTextBox.Document.Blocks.Add(messageParagraph);

                // Ensure the latest message is visible
                messagesTextBox.ScrollToEnd();
            });
        }


        private void OnKeyExchangeUpdate(string status, byte[]? sharedSecret, bool clean = false)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateSharedSecretStatus(status, clean);
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
        public void UpdateSharedSecretStatus(string status, bool clean = false)
        {
            if (clean)
            {
                keyExchangeStatusText.Text = $"[{DateTime.Now}] - " + status;
            }
            else
            {
                keyExchangeStatusText.Text += "\n" + $"[{DateTime.Now}] - " + status;
            }

        }

        public void UpdateSharedSecretDisplay(byte[] sharedSecret)
        {
            sharedSecretText.Text = $"Updated [{DateTime.Now}] - " + BitConverter.ToString(sharedSecret).Replace("-", "");
        }

        private void OnSentMessage(string message)
        {
            DisplayMessage($"{message}", true);
        }

        private void OnMessageReceivedAlice(string message)
        {
            Dispatcher.Invoke(() => DisplayMessage($"{message}", false));
        }
        private void OnMessageReceivedBob(string message)
        {
            Dispatcher.Invoke(() => DisplayMessage($"{message}", false));
        }

    }
}