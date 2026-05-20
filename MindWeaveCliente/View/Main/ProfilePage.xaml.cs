using MindWeaveClient.ViewModel.Main;
using System.Windows.Controls;

namespace MindWeaveClient.View.Main
{
    public partial class ProfilePage : Page
    {
        public ProfilePage(ProfileViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;
        }
    }
}