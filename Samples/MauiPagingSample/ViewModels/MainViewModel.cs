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

        private IAsyncRelayCommand<string>? openUrlCommand;

        public MainViewModel(
            ILogger<MainViewModel> logger,
            ICarService carService,
            ILauncher launcher)
        {
            this.logger = logger;
            this.carService = carService;
            this.launcher = launcher;

            // The collection owns the PagingInfo, advances the page, maps each Car to a
            // CarItemViewModel and decides when to stop - so the view model only needs to say
            // how to load a page and how to project it.
            this.Cars = new InfiniteScrollCollection<CarItemViewModel>(new PagingInfo { ItemsPerPage = 30 })
                .WithPageLoader(this.carService.GetCarsAsync)
                .WithMapping(car => new CarItemViewModel(car))
                .OnError(ex => this.logger.LogError(ex, "Failed to load cars"));

            _ = this.Cars.InitializeAsync();
        }

        public InfiniteScrollCollection<CarItemViewModel> Cars { get; }

        public IAsyncRelayCommand<string> OpenUrlCommand
        {
            get => this.openUrlCommand ??= new AsyncRelayCommand<string>(this.OpenUrlAsync!);
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