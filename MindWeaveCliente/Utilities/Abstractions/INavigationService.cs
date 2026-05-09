using System.Windows.Controls;

namespace MindWeaveClient.Utilities.Abstractions
{
    public interface INavigationService
    {
        void initialize(Frame navigationFrame);

        void navigateTo<TView>() where TView : Page;

        void goBack();
    }
}