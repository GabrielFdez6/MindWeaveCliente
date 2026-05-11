using MindWeaveClient.ViewModel.Authentication;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace MindWeaveClient.View.Authentication
{
    public partial class LoginPage : Page
    {
        public LoginPage(LoginViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void toggleShowPassOnClick(object sender, RoutedEventArgs e)
        {
            bool isVisible = ToggleShowPass.IsChecked == true;

            if (isVisible)
            {
                txtVisiblePassword.Focus();
                txtVisiblePassword.CaretIndex = txtVisiblePassword.Text.Length;
            }
            else
            {
                pbPassword.Focus();
                SetPasswordBoxCaretToEnd(pbPassword);
            }
        }

        [SuppressMessage("Major Code Smell", "S3011:Reflection should not be used to increase accessibility",
            Justification = "WPF PasswordBox does not expose public API for caret positioning. This is a known limitation.")]
        private static void SetPasswordBoxCaretToEnd(PasswordBox passwordBox)
        {
            try
            {
                var selectMethod = typeof(PasswordBox).GetMethod("Select",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                selectMethod?.Invoke(passwordBox, new object[] { passwordBox.Password.Length, 0 });
            }
            catch
            {
                // ignored
            }
        }
    }
}