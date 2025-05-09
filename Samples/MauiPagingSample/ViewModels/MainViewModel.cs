using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiPagingSample.Model;
using MauiPagingSample.Services;
using Microsoft.Extensions.Logging;
using Paging;
using Paging.MAUI;

namespace MauiPagingSample.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly ILogger logger;
        private readonly ICarService carService;
        private readonly ILauncher launcher;
        private readonly PagingInfo pagingInfo;

        private bool isLoadingMore;
        private PaginationSet<Car> lastPaginationSet;
        private InfiniteScrollCollection<CarItemViewModel> cars;
        private IAsyncRelayCommand<string> openUrlCommand;

        public MainViewModel(
            ILogger<MainViewModel> logger,
            ICarService carService,
            ILauncher launcher)
        {
            this.logger = logger;
            this.carService = carService;
            this.launcher = launcher;

            this.pagingInfo = new PagingInfo
            {
                CurrentPage = 1,
                ItemsPerPage = 30,
            };

            this.Cars = new InfiniteScrollCollection<CarItemViewModel>();

            _ = this.LoadData();
        }

        public InfiniteScrollCollection<CarItemViewModel> Cars
        {
            get => this.cars;
            private set => this.SetProperty(ref this.cars, value);
        }

        public bool IsLoadingMore
        {
            get => this.isLoadingMore;
            set => this.SetProperty(ref this.isLoadingMore, value);
        }

        private async Task LoadData()
        {
            try
            {
                this.logger.LogDebug($"LoadData: CurrentPage={this.pagingInfo.CurrentPage}");

                this.Cars.OnCanLoadMore = () => !this.lastPaginationSet.StopScroll(this.pagingInfo);
                this.Cars.OnLoadMore = async () =>
                {
                    var paginationSet = await this.carService.GetCarsAsync(this.pagingInfo);
                    this.lastPaginationSet = paginationSet;

                    this.logger.LogDebug(
                        $"OnLoadMore: Page {paginationSet.CurrentPage} of {paginationSet.TotalPages}, Items={paginationSet.Items.Count()}");

                    this.pagingInfo.CurrentPage++;

                    var carViewModels = paginationSet.Items
                        .Select(car => new CarItemViewModel(car))
                        .ToArray();

                    return carViewModels;
                };

                await this.Cars.LoadMoreAsync();
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to init viewmodel");
            }
        }


        public IAsyncRelayCommand<string> OpenUrlCommand
        {
            get => this.openUrlCommand ??= new AsyncRelayCommand<string>(this.OpenUrlAsync);
        }

        private async Task OpenUrlAsync(string url)
        {
            try
            {
                await this.launcher.TryOpenAsync(url);
            }
            catch
            {
                // Ignore exceptions
            }
        }
    }
}