using MindWeaveClient.ViewModel.Authentication;
using System.Windows.Controls;

namespace MindWeaveClient.View.Authentication
{
    public partial class CreateAccountPage : Page
    {
        public CreateAccountPage(CreateAccountViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;
        }
    }
}