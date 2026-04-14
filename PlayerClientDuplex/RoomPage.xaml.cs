using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SharedContracts;

namespace PlayerClientDuplex
{
    public partial class RoomPage : Page
    {
        private readonly string _username;
        public string RoomName { get; }
        private readonly RoomCallbackHandler _callbackHandler;

        public RoomPage(string username, string room, RoomCallbackHandler callbackHandler)
        {
            InitializeComponent();

            _username = username;
            RoomName = room;
            _callbackHandler = callbackHandler;
            _callbackHandler.SetRoomPage(this);

            lblRoomName.Text = $"Room: {RoomName}";

            JoinRoom();
        }
        

        public string getUsername()
        {
            return _username;
        }

        private async void JoinRoom()
        {
            try
            {
                // Run WCF calls off the UI thread
                var snapshot = await Task.Run(() =>
                {
                    ServiceClient.Instance.Proxy.JoinRoomDuplex(_username, RoomName);
                    return ServiceClient.Instance.Proxy.GetRoomSnapshot(RoomName);
                });

                // Update UI safely
                Dispatcher.Invoke(() =>
                {
                    lstUsers.ItemsSource = snapshot.Users;
                    lstFiles.ItemsSource = snapshot.Files.Select(f => f.FileName).ToList();

                    lstMessages.Items.Clear();
                    foreach (var msg in snapshot.Messages)
                    {
                        lstMessages.Items.Add(msg.IsFileLink
                            ? $"{msg.From} shared file: {msg.FileName}"
                            : $"{msg.From}: {msg.Text}");
                    }

                    if (lstMessages.Items.Count > 0)
                        lstMessages.ScrollIntoView(lstMessages.Items[lstMessages.Items.Count - 1]);
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                    MessageBox.Show("Error joining room: " + ex.Message));
            }
        }

        public void AddMessage(ChatMessage msg)
        {
            if (msg == null) return;
            Dispatcher.Invoke(() =>
            {
                lstMessages.Items.Add(msg.IsFileLink
                    ? $"{msg.From} shared file: {msg.FileName}"
                    : $"{msg.From}: {msg.Text}");
                lstMessages.ScrollIntoView(lstMessages.Items[lstMessages.Items.Count - 1]);
            });
        }

        public void UpdateUserList(List<string> users)
        {
            Dispatcher.Invoke(() => lstUsers.ItemsSource = users);
        }

        public void AddFile(FileMeta meta)
        {
            Dispatcher.Invoke(() =>
            {
                var current = lstFiles.Items.Cast<string>().ToList();
                if (!current.Contains(meta.FileName))
                {
                    current.Add(meta.FileName);
                    lstFiles.ItemsSource = null;
                    lstFiles.ItemsSource = current;
                }
            });
        }

        private async void btnSend_Click(object sender, RoutedEventArgs e)
        {
            string text = txtMessage.Text?.Trim();
            if (string.IsNullOrEmpty(text)) return;

            var msg = new ChatMessage
            {
                From = _username,
                Room = RoomName,
                Text = text,
                TimestampUtc = DateTime.UtcNow
            };

            txtMessage.Clear();

            try
            {
                // Only send to server; don't add locally
                await Task.Run(() => ServiceClient.Instance.Proxy.SendMessageDuplex(msg));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show("Error sending message: " + ex.Message));
            }
        }

        private async void btnUpload_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Images and Text|*.png;*.jpg;*.jpeg;*.gif;*.txt"
            };
            if (dlg.ShowDialog() != true) return;

            byte[] data;
            using (var fs = new FileStream(dlg.FileName, FileMode.Open, FileAccess.Read))
            {
                data = new byte[fs.Length];
                await fs.ReadAsync(data, 0, data.Length);
            }

            var meta = new FileMeta
            {
                FileName = Path.GetFileName(dlg.FileName),
                Room = RoomName,
                Uploader = _username
            };

            // Upload file; callback will trigger AddFile
            await Task.Run(() => ServiceClient.Instance.Proxy.UploadFile(meta, data));
        }

        private async void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            if (!(lstFiles.SelectedItem is string fileName)) return;

            var files = await Task.Run(() => ServiceClient.Instance.Proxy.ListFiles(RoomName));
            var meta = files.FirstOrDefault(f => f.FileName == fileName);
            if (meta == null) return;

            var bytes = await Task.Run(() => ServiceClient.Instance.Proxy.DownloadFile(meta.FileId));
            if (bytes == null || bytes.Length == 0) return;

            var saveDlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = meta.FileName,
                Filter = "All files|*.*",
                DefaultExt = Path.GetExtension(meta.FileName)
            };

            if (saveDlg.ShowDialog() == true)
                await Task.Run(() => File.WriteAllBytes(saveDlg.FileName, bytes));
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            try { ServiceClient.Instance.Proxy.LeaveRoomDuplex(_username, RoomName); } catch { }
            if (NavigationService?.CanGoBack == true) NavigationService.GoBack();
        }

        private void lstUsers_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (lstUsers.SelectedItem is string otherUser && otherUser != _username)
            {
                NavigationService?.Navigate(new PrivateChatPage(_username, otherUser, RoomName));
            }
        }
    }
}