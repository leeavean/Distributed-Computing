using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using SharedContracts;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for Page1.xaml
    /// </summary>
    public partial class RoomListPage : Page
    {
        private readonly string _username;

        public RoomListPage(string username)
        {
            InitializeComponent();
            _username = username;
            LoadRooms();
        }

        private void LoadRooms()
        {
            List<RoomInfo> rooms = ServiceClient.Instance.ListRooms();
            lstRooms.ItemsSource = rooms;
        }

        private void btnCreateRoom_Click(object sender, RoutedEventArgs e)
        {
            string newRoom = txtNewRoom.Text.Trim();
            if (!string.IsNullOrEmpty(newRoom))
            {
                bool created = ServiceClient.Instance.CreateRoom(newRoom);
                if (created)
                {
                    LoadRooms();
                }
                else
                {
                    MessageBox.Show("Room already exists.");
                }
            }
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            // unregister the user from the service
            try
            {
                ServiceClient.Instance.Logout(_username);
            }
            catch
            {
                // ignore errors if service call fails
            }

            // Navigate back to login page
            NavigationService.Navigate(new LoginPage());
        }

        private void lstRooms_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnJoinRoom.IsEnabled = lstRooms.SelectedItem != null;
        }

        private void btnJoinRoom_Click(object sender, RoutedEventArgs e)
        {
            if (lstRooms.SelectedItem is RoomInfo room)
            {
                bool ok = ServiceClient.Instance.JoinRoom(_username, room.RoomName);
                if (ok)
                {
                    NavigationService.Navigate(new RoomPage(_username, room.RoomName));
                }
                else
                {
                    MessageBox.Show("Could not join room.");
                }
            }
        }
    }
}
