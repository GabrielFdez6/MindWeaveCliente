using MindWeaveClient.Utilities.Abstractions;
using MindWeaveClient.View.Dialogs;
using System.Linq;
using System.Windows;

namespace MindWeaveClient.Utilities.Implementations
{
    public class DialogService : IDialogService
    {
        public void showInfo(string message, string title)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void showError(string message, string title)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public bool showWarning(string message, string title)
        {
            return MessageBox.Show(message, title, MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK;
        }

        public bool showConfirmation(string message, string title)
        {
            return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
        }

        public bool showGuestInputDialog(out string email)
        {
            var dialog = new GuestInputDialog();
            dialog.Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);

            bool? result = dialog.ShowDialog();
            email = (result == true) ? dialog.GuestEmail : null;
            return result == true;
        }
    }
}
