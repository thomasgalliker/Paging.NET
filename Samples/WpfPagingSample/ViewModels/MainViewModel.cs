using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Paging;
using WpfPagingSample.Model;
using WpfPagingSample.Services;

namespace WpfPagingSample.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IEmployeeService employeeRepository;
        private readonly PagingInfo pagingInfo;

        private ObservableCollection<Employee> employees;
        private int currentPage = 1;

        public MainViewModel(IEmployeeService employeeService)
        {
            this.employeeRepository = employeeService;

            this.pagingInfo = new PagingInfo
            {
                CurrentPage = currentPage,
                ItemsPerPage = 10,
            };

            this.PreviousPageCommand = new DelegateCommand<object>((o) => this.GoToPreviousPage());
            this.NextPageCommand = new DelegateCommand<object>((o) => this.GoToNextPage());

            this.DisplayAllEmployees(this.pagingInfo);
        }

        public ICommand PreviousPageCommand { get; }

        private void GoToPreviousPage()
        {
            // TODO: Check if lower bound is reached
            this.CurrentPage--;
            this.DisplayAllEmployees(this.pagingInfo);
        }

        public ICommand NextPageCommand { get; }

        private void GoToNextPage()
        {
            // TODO: Check if upper bound is reached
            this.CurrentPage++;
            this.DisplayAllEmployees(this.pagingInfo);
        }

        public int CurrentPage
        {
            get => this.currentPage;
            private set
            {
                this.currentPage = value;
                this.OnPropertyChanged(nameof(this.CurrentPage));
                this.pagingInfo.CurrentPage = value;
            }
        }

        private async void DisplayAllEmployees(PagingInfo pagingInfo)
        {
            try
            {
                var paginationSet = await this.employeeRepository.GetEmployeesAsync(pagingInfo);
                this.Employees = new ObservableCollection<Employee>(paginationSet.Items);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load employees: {ex.Message}");
            }
        }

        public ObservableCollection<Employee> Employees
        {
            get => this.employees;
            private set
            {
                if (this.employees != value)
                {
                    this.employees = value;
                    this.OnPropertyChanged(nameof(this.Employees));
                }
            }
        }
    }
}
