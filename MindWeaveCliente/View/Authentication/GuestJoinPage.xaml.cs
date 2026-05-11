using MindWeaveClient.ViewModel.Authentication;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;

namespace MindWeaveClient.View.Authentication
{
    public partial class GuestJoinPage : Page
    {
        public GuestJoinPage(GuestJoinViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;
        }
    }
}