using MauiPagingSample.Model;
using Paging;

namespace MauiPagingSample.Services
{
    public interface ICarService
    {
        Task<PaginationSet<Car>> GetCarsAsync(PagingInfo pageInfo);
    }
}