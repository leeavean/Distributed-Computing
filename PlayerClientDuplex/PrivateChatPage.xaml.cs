using SharedContracts;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PlayerClientDuplex
{
    public partial class PrivateChatPage : Page
    {
        private readonly string _me;
        private readonly string _other;
        private readonly string _room;

        public PrivateChatPage(string me, string other, string room)
        {
            InitializeComponent();
            _me = me ?? throw new ArgumentNullException(nameof(me));
            _other = other ?? throw new ArgumentNullException(nameof(other));
            _room = room ?? throw new ArgumentNullException(nameof(room));

            lblChatTitle.Content = $"Private Chat: {_me} ↔ {_other}";
        }

        public void AddMessage(ChatMessage msg)
        {
            if (msg == null) return;

            lstMessages.Dispatcher.Invoke(() =>
            {
                lstMessages.Items.Add(msg.IsFileLink
                    ? $"{msg.From} shared file: {msg.FileName}"
                    : $"{msg.From}: {msg.Text}");

                if (lstMessages.Items.Count > 0)
                    lstMessages.ScrollIntoView(lstMessages.Items[lstMessages.Items.Count - 1]);
            });
        }

        private async void OnSendClicked(object sender, RoutedEventArgs e)
        {
            string text = txtMessage.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            var msg = new ChatMessage
            {
                From = _me,
                To = _other,
                Room = _room,
                Text = text,
                TimestampUtc = DateTime.UtcNow
            };

            txtMessage.Clear();

            // Update UI locally immediately
            AddMessage(msg);

            try
            {
                // Send message asynchronously
                await Task.Run(() => ServiceClient.Instance.Proxy.SendMessageDuplex(msg));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error sending private message: " + ex.Message);
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
                NavigationService.GoBack();
        }
    }
}