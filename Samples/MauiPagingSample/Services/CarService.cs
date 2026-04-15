using MauiPagingSample.Model;
using Microsoft.Extensions.Logging;
using Paging;

namespace MauiPagingSample.Services
{
    public class CarService : ICarService
    {
        private readonly ILogger logger;
        private readonly int itemsCount;

        public CarService(ILogger<CarService> logger)
        {
            this.logger = logger;
            this.itemsCount = 1000;
        }

        public async Task<PaginationSet<Car>> GetCarsAsync(PagingInfo pageInfo)
        {
            this.logger.LogDebug($"GetCarsAsync: CurrentPage={pageInfo.CurrentPage}, ItemsPerPage={pageInfo.ItemsPerPage}");
            await Task.Delay(1000);

            var itemsPerPage = pageInfo.ItemsPerPage ?? this.itemsCount;
            var pageOffset = pageInfo.CurrentPage - pageInfo.FirstPageIndex;
            var from = pageOffset * itemsPerPage + 1;
            var remainingItems = this.itemsCount - pageOffset * itemsPerPage;
            var count = remainingItems < itemsPerPage
                ? remainingItems
                : itemsPerPage;

            var items = GenerateCarsList(from, count).ToArray();
            var pagingSet = new PaginationSet<Car>(pageInfo, items, this.itemsCount, this.itemsCount);

            return pagingSet;
        }

        private static IEnumerable<Car> GenerateCarsList(int start, int count)
        {
            var end = start + count;
            for (var i = start; i < end; i++)
            {
                yield return new Car
                {
                    Id = i,
                    Name = $"Car {i}"
                };
            }
        }
    }
}
