using Microsoft.Extensions.DependencyInjection;
using MindWeaveClient.Utilities.Abstractions;
using System;
using System.Windows.Controls;

namespace MindWeaveClient.Utilities.Implementations
{
    public class NavigationService : INavigationService
    {
        private readonly IServiceProvider serviceProvider;
        private Frame navigationFrame;

        public NavigationService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public void initialize(Frame navigationFrame)
        {
            this.navigationFrame = navigationFrame;
        }

        public void navigateTo<TView>() where TView : Page
        {
            var page = serviceProvider.GetService<TView>();

            navigationFrame.Navigate(page);
        }

        public void goBack()
        {
            if (navigationFrame != null && navigationFrame.CanGoBack)
            {
                navigationFrame.GoBack();
            }
        }
    }
}
