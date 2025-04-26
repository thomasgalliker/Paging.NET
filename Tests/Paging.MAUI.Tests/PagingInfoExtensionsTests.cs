using Xunit;

namespace Paging.MAUI.Tests
{
    public class PagingInfoExtensionsTests
    {
        [Fact]
        public void Test()
        {
            ////// Arrange
            ////var queryable = CarFactory.GenerateCarsList(10).AsQueryable();
            ////var pagingInfo = new PagingInfo { CurrentPage = 1, ItemsPerPage = 4 };

            ////// Act
            ////var paginationSet = pagingInfo.CreatePaginationSet<Car, CarDto>(queryable, CarFactory.MapCarsToCarDtos, c => true);

            ////// Assert
            ////paginationSet.Should().NotBeNull();
            ////paginationSet.Items.Should().HaveCount(4);
            ////paginationSet.CurrentPage.Should().Be(1);
            ////paginationSet.TotalPages.Should().Be(3);
            ////paginationSet.TotalCount.Should().Be(10);
            ////paginationSet.TotalCountUnfiltered.Should().Be(10);
        }
    }
}
