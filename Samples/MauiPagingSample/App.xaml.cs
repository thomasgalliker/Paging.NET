using MauiPagingSample.Views;

namespace MauiPagingSample
{
    public partial class App : Application
    {
        public App(IServiceProvider serviceProvider)
        {
            this.InitializeComponent();

            var mainPage = serviceProvider.GetRequiredService<MainPage>();
            this.MainPage = mainPage;
        }
    }
}