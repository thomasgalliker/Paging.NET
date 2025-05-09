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

            var from = (pageInfo.CurrentPage - 1) * pageInfo.ItemsPerPage + 1;
            var remainingItems = this.itemsCount - (pageInfo.CurrentPage - 1) * pageInfo.ItemsPerPage;
            var count = remainingItems < pageInfo.ItemsPerPage
                ? remainingItems
                : pageInfo.ItemsPerPage;

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