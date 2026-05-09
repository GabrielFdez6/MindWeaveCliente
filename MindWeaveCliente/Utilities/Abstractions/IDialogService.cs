namespace MindWeaveClient.Utilities.Abstractions
{
    public interface IDialogService
    {
        void showInfo(string message, string title);
        void showError(string message, string title);
        bool showWarning(string message, string title);
        bool showConfirmation(string message, string title);
        bool showGuestInputDialog(out string email);
    }
}