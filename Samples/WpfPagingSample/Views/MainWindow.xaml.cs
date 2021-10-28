using System.Windows;
using WpfPagingSample.Services;
using WpfPagingSample.ViewModels;

namespace WpfPagingSample.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.DataContext = new MainViewModel(new EmployeeServiceStub());
        }
    }
}
