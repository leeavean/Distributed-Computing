using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for Page1.xaml
    /// </summary>
    public partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            if (string.IsNullOrEmpty(username))
            {
                MessageBox.Show("Enter a username.");
                return;
            }

            bool ok = ServiceClient.Instance.Login(username);
            if (ok)
            {
                // Navigate to room list page
                NavigationService.Navigate(new RoomListPage(username));
            }
            else
            {
                MessageBox.Show("Login failed. Username may already be taken.");
            }
        }
    }
}
