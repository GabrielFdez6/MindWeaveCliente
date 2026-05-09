using MindWeaveClient.ViewModel.Authentication;
using System.Windows.Controls;

namespace MindWeaveClient.View.Authentication
{
    public partial class PasswordRecoveryPage : Page
    {
        public PasswordRecoveryPage(PasswordRecoveryViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;
        }
    }
}