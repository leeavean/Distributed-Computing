using SharedContracts;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace WpfApp1
{
    public partial class RoomPage : Page
    {
        private readonly string _username;
        private readonly string _room;
        private readonly DispatcherTimer _updateTimer;

        public RoomPage(string username, string room)
        {
            InitializeComponent();
            _username = username;
            _room = room;
            lblRoomName.Text = $"Room: {_room}";

            // Timer for periodic updates (polling)
            _updateTimer = new DispatcherTimer();
            _updateTimer.Interval = TimeSpan.FromSeconds(1); // update every second
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();
        }

        private async void UpdateTimer_Tick(object sender, EventArgs e)
        {
            await RefreshRoomData();
        }

        private async Task RefreshRoomData()
        {
            try
            {
                // Get messages
                var updates = await Task.Run(() => ServiceClient.Instance.GetUpdates(_username, DateTime.UtcNow.AddMinutes(-10)));
                lstMessages.Items.Clear();
                foreach (var msg in updates.Messages)
                {
                    lstMessages.Items.Add(msg.IsFileLink
                        ? $"{msg.From} shared file: {msg.FileName}"
                        : $"{msg.From}: {msg.Text}");
                }

                // Users
                lstUsers.ItemsSource = updates.PlayerList;

                // Files
                lstFiles.ItemsSource = updates.Files.Select(f => f.FileName).ToList();
            }
            catch
            {
                // ignore errors in polling
            }
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            string text = txtMessage.Text.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                var msg = new ChatMessage
                {
                    From = _username,
                    Room = _room,
                    Text = text
                };
                ServiceClient.Instance.SendMessage(msg);
                txtMessage.Clear();
            }
        }

        private void lstUsers_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (lstUsers.SelectedItem is PlayerInfo user && user.Username != _username)
            {
                NavigationService.Navigate(new PrivateChatPage(_username, user.Username, _room));
            }
        }

        private void btnUpload_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "Images and Text|*.png;*.jpg;*.jpeg;*.gif;*.txt";
            if (dlg.ShowDialog() == true)
            {
                byte[] fileData;
                using (var fs = new FileStream(dlg.FileName, FileMode.Open, FileAccess.Read))
                {
                    fileData = new byte[fs.Length];
                    fs.Read(fileData, 0, fileData.Length);
                }
                var meta = new FileMeta
                {
                    FileName = System.IO.Path.GetFileName(dlg.FileName),
                    Room = _room,
                    Uploader = _username
                };
                ServiceClient.Instance.UploadFile(meta, fileData);
            }
        }

        private void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            if (lstFiles.SelectedItem is string fileName)
            {
                FileStream fs = null;
                Stream stream = null;

                try
                {
                    // Get the file ID and metadata
                    var fileId = ServiceClient.Instance.GetFileId(_room, fileName);
                    var fileMeta = ServiceClient.Instance.ListFiles(_room, DateTime.MinValue)
                                      .FirstOrDefault(f => f.FileId == fileId);

                    if (fileMeta == null)
                    {
                        MessageBox.Show("File metadata not found.");
                        return;
                    }

                    stream = ServiceClient.Instance.DownloadFile(fileId);

                    // Configure SaveFileDialog with proper filter and extension
                    var saveDlg = new Microsoft.Win32.SaveFileDialog
                    {
                        FileName = fileMeta.FileName,
                        Filter = "All files|*.*|Images|*.png;*.jpg;*.jpeg;*.gif|Text files|*.txt",
                        DefaultExt = Path.GetExtension(fileMeta.FileName)
                    };

                    if (saveDlg.ShowDialog() == true)
                    {
                        fs = new FileStream(saveDlg.FileName, FileMode.Create, FileAccess.Write);
                        stream.CopyTo(fs);
                        MessageBox.Show("File downloaded successfully.");
                    }
                }
                finally
                {
                    if (fs != null)
                        fs.Dispose();
                    if (stream != null)
                        stream.Dispose();
                }
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            // Navigate back to previous page, e.g., main lobby
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();

    }
}
}

