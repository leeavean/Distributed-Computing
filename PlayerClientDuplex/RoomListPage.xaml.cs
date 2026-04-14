using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using SharedContracts;

namespace PlayerClientDuplex
{
    public partial class RoomListPage : Page
    {
        private readonly string _username;
        private readonly RoomCallbackHandler _callbackHandler;

        public RoomListPage(string username, RoomCallbackHandler callbackHandler)
        {
            InitializeComponent();
            _username = username;
            _callbackHandler = callbackHandler;

            Loaded += RoomListPage_Loaded;
        }

        private void RoomListPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadRooms();
        }

        private void LoadRooms()
        {
            try
            {
                var rooms = ServiceClient.Instance.ListRooms();
                lstRooms.ItemsSource = null;   // clear binding
                lstRooms.ItemsSource = rooms;  // rebind
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading rooms: " + ex.Message);
            }
        }

        private void btnCreateRoom_Click(object sender, RoutedEventArgs e)
        {
            string newRoom = txtNewRoom.Text.Trim();
            if (string.IsNullOrEmpty(newRoom)) return;

            try
            {
                if (ServiceClient.Instance.Proxy.CreateRoomDuplex(newRoom))
                {
                    MessageBox.Show("Room created successfully!");
                    LoadRooms(); // refresh list
                    txtNewRoom.Clear();
                }
                else
                {
                    MessageBox.Show("Room already exists.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error creating room: " + ex.Message);
            }
        }

        private void btnJoinRoom_Click(object sender, RoutedEventArgs e)
        {
            if (lstRooms.SelectedItem is RoomInfo room)
            {
                try
                {
                    var roomPage = new RoomPage(_username, room.RoomName, _callbackHandler);
                    _callbackHandler.SetRoomPage(roomPage);
                    NavigationService?.Navigate(roomPage);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error joining room: " + ex.Message);
                }
            }
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            ServiceClient.Instance.Logout(_username);
            NavigationService.Navigate(new LoginPage());
        }

        private void lstRooms_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnJoinRoom.IsEnabled = lstRooms.SelectedItem != null;
        }
    }
}