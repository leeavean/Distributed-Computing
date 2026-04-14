using System;
using System.Windows;
using System.Windows.Controls;

namespace PlayerClientDuplex
{
    public partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            if (string.IsNullOrEmpty(username)) return;

            try
            {
                // 1️⃣ Create concrete callback handler
                var callbackHandler = new RoomCallbackHandler();

                // 2️⃣ Initialize singleton ServiceClient
                ServiceClient.Initialize(callbackHandler);

                // 3️⃣ Register user on server
                ServiceClient.Instance.Proxy.Register(username);

                // 4️⃣ Navigate to RoomListPage and pass the concrete callback
                NavigationService.Navigate(new RoomListPage(username, callbackHandler));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Login failed: " + ex.Message);
            }
        }

    }
}