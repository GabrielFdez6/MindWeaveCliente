using MindWeaveClient.ViewModel.Main;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MindWeaveClient.View.Main
{
    public partial class SocialPage : Page
    {
        public SocialPage(SocialViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;

            this.Unloaded += SocialPage_Unloaded;
        }

        private void SocialPage_Unloaded(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is SocialViewModel vm)
            {
                vm.cleanup();
            }
            this.Unloaded -= SocialPage_Unloaded;
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && this.DataContext is SocialViewModel vm && vm.SearchCommand.CanExecute(null))
            {
                vm.SearchCommand.Execute(null);
            }
        }
    }
}