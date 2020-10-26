using System;
using System.Security.Cryptography;
using System.Text;

using ITinnovationsLibrary.Functions;

namespace Chimera_Importer.Authentication
{
    /// <summary>
    ///     Interaction logic for AuthenticationWindow.xaml
    /// </summary>
    internal partial class AuthenticationWindow
    {
        public string Username => UsernameBox.Text;

        public string PasswordMd5 => EncodePassword(PasswordCtrl.Password);

        public AuthenticationWindow()
        {
            InitializeComponent();
        }

        public string EncodePassword(string originalPassword)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(originalPassword))
                {
                    byte[] originalBytes = Encoding.Default.GetBytes(originalPassword);

                    MD5 md5 = new MD5CryptoServiceProvider();
                    byte[] encodedBytes = md5.ComputeHash(originalBytes);

                    return BitConverter.ToString(encodedBytes).Replace("-", "");
                }
                else
                {
                    Message.ErrorMessage("Error while encrypting password: the password is empty!");

                    return null;
                }
            }
            catch (Exception ex)
            {
                Message.ErrorMessage("Unexpected error while encrypting password!\n\n" + ex.Message);

                return null;
            }
        }

        private void OKBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                Message.ErrorMessage("You must insert a username!");
            }
            else if (string.IsNullOrWhiteSpace(PasswordCtrl.Password))
            {
                Message.ErrorMessage("You must insert a password!");
            }
            else
            {
                DialogResult = true;

                Close();
            }
        }
    }
}
