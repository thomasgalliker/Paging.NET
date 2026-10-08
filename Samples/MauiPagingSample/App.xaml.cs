using MauiPagingSample.Views;

namespace MauiPagingSample
{
    public partial class App : Application
    {
        private readonly IServiceProvider serviceProvider;

        public App(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            this.InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            Page mainPage = this.serviceProvider.GetRequiredService<MainPage>();

            // Uncomment to run the legacy ListView-based page (Paging.MAUI.Compat):
            // Page mainPage = this.serviceProvider.GetRequiredService<MainPage_WithListView>();

            return new Window(new NavigationPage(mainPage));
        }
    }
}