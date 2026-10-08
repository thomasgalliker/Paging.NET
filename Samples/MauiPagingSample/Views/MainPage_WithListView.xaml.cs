using MauiPagingSample.ViewModels;

namespace MauiPagingSample.Views
{
    public partial class MainPage_WithListView : ContentPage
    {
        public MainPage_WithListView(MainViewModel mainViewModel)
        {
            this.InitializeComponent();
            this.BindingContext = mainViewModel;
        }
    }
}
