using SharedContracts;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace WpfApp1
{
    public partial class PrivateChatPage : Page
    {
        private readonly string _me;
        private readonly string _other;
        private readonly string _room;
        private readonly DispatcherTimer _updateTimer;
        private DateTime _lastUpdateUtc = DateTime.MinValue;

        public PrivateChatPage(string me, string other, string room)
        {
            InitializeComponent();
            _me = me;
            _other = other;
            _room = room;

            lblChatTitle.Content = $"Private Chat: {_me} ↔ {_other}";

            // Timer for polling new messages every second
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();
        }

        private async void UpdateTimer_Tick(object sender, EventArgs e)
        {
            await FetchNewMessages();
        }

        private async Task FetchNewMessages()
        {
            try
            {
                // Get private messages since last update
                var messages = await Task.Run(() =>
                    ServiceClient.Instance.GetPrivateMessages(_me, _other, _lastUpdateUtc));

                foreach (var msg in messages.OrderBy(m => m.TimestampUtc))
                {
                    lstMessages.Items.Add($"{msg.From}: {msg.Text}");
                    lstMessages.ScrollIntoView(lstMessages.Items[lstMessages.Items.Count - 1]);
                    _lastUpdateUtc = msg.TimestampUtc;
                }
            }
            catch
            {
                // ignore server errors
            }
        }

        private void OnSendClicked(object sender, RoutedEventArgs e)
        {
            string text = txtMessage.Text.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                var msg = new ChatMessage
                {
                    From = _me,
                    To = _other,
                    Room = _room,
                    Text = text,
                    TimestampUtc = DateTime.UtcNow
                };

                // Send to server
                ServiceClient.Instance.SendMessage(msg);

                // Show immediately in chat
                lstMessages.Items.Add($"{msg.From}: {msg.Text}");
                lstMessages.ScrollIntoView(lstMessages.Items[lstMessages.Items.Count - 1]);

                txtMessage.Clear();
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            _updateTimer.Stop();
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
        }
    }
}

