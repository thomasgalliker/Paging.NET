namespace Paging.Tests
{
    public class PaginationSetExtensionsTests
    {
        [Fact]
        public void ShouldMapToPaginationSet()
        {
            // Arrange
            var pagingInfo = new PagingInfo { CurrentPage = 2, ItemsPerPage = 3 };
            var cars = CarFactory.GenerateCarsList(3).ToArray();
            var source = new PaginationSet<Car>(pagingInfo, cars, totalCount: 10, totalCountUnfiltered: 12);

            // Act
            var target = source.Map(CarFactory.MapCarsToCarDtos);

            // Assert
            target.Should().BeOfType<PaginationSet<CarDto>>();
            target.FirstPageIndex.Should().Be(source.FirstPageIndex);
            target.CurrentPage.Should().Be(source.CurrentPage);
            target.TotalPages.Should().Be(source.TotalPages);
            target.TotalCount.Should().Be(source.TotalCount);
            target.TotalCountUnfiltered.Should().Be(source.TotalCountUnfiltered);
            target.Items.Should().BeEquivalentTo(cars.Select(c => new CarDto { Id = c.Id, Name = c.Name }));
        }

        [Fact]
        public void ShouldMapToPaginationSet_Empty()
        {
            // Arrange
            var pagingInfo = new PagingInfo { CurrentPage = 1, ItemsPerPage = 0 };
            var cars = Array.Empty<Car>();
            var source = new PaginationSet<Car>(pagingInfo, cars, totalCount: 0, totalCountUnfiltered: 0);

            // Act
            var target = source.Map(CarFactory.MapCarsToCarDtos);

            // Assert
            target.TotalPages.Should().Be(0);
            target.TotalCount.Should().Be(0);
            target.TotalCountUnfiltered.Should().Be(0);
            target.Items.Should().BeEmpty();
        }

        [Fact]
        public void ShouldThrowArgumentNullExceptionIfMappingIsNull()
        {
            // Arrange
            PaginationSet<Car>? source = null;

            // Act
            Action action = () => source!.Map(CarFactory.MapCarsToCarDtos);

            // Assert
            var argumentNullException = action.Should().Throw<ArgumentNullException>() .Which;
            argumentNullException.ParamName.Should().Be("paginationSet");
        }

        [Fact]
        public void ShouldThrowArgumentNullExceptionIfPaginationSetIsNull()
        {
            // Arrange
            var pagingInfo = new PagingInfo { CurrentPage = 1, ItemsPerPage = 0 };
            var cars = Array.Empty<Car>();
            var source = new PaginationSet<Car>(pagingInfo, cars,  totalCount: 0, totalCountUnfiltered: 0);

            // Act
            Action action = () => source.Map<Car, CarDto>(null!);

            // Assert
            var argumentNullException = action.Should().Throw<ArgumentNullException>() .Which;
            argumentNullException.ParamName.Should().Be("map");
        }

        [Fact]
        public void CanLoadMore_ReturnsTrue_WhenPaginationSetIsNull()
        {
            // Arrange
            PaginationSet<Car>? paginationSet = null;
            var pagingInfo = new PagingInfo { CurrentPage = 1, ItemsPerPage = 3 };

            // Act
            var canLoadMore = paginationSet.CanLoadMore(pagingInfo);

            // Assert
            canLoadMore.Should().BeTrue();
        }

        [Theory]
        [InlineData(1, 3, 10, true)]    // first page loaded of several -> more available
        [InlineData(3, 3, 10, true)]    // 9 of 10 items loaded -> more available
        [InlineData(4, 3, 10, false)]   // 12 >= 10 items loaded -> nothing left
        [InlineData(1, null, 10, false)] // unpaged single result -> nothing more to load
        [InlineData(1, 0, 10, false)]    // totals-only request -> no items to scroll
        public void CanLoadMore_ReturnsExpected_ForItemsPerPage(int currentPage, int? itemsPerPage, int totalCount, bool expected)
        {
            // Arrange
            var pagingInfo = new PagingInfo { CurrentPage = currentPage, ItemsPerPage = itemsPerPage };
            var paginationSet = new PaginationSet<Car>(pagingInfo, Array.Empty<Car>(), totalCount, totalCount);

            // Act
            var canLoadMore = paginationSet.CanLoadMore(pagingInfo);

            // Assert
            canLoadMore.Should().Be(expected);
        }

        [Fact]
        public void CanLoadMore_Throws_WhenPagingInfoIsNull()
        {
            // Arrange
            var paginationSet = new PaginationSet<Car>(new PagingInfo(), Array.Empty<Car>(), 0, 0);

            // Act
            Action action = () => paginationSet.CanLoadMore(null!);

            // Assert
            action.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("pagingInfo");
        }
    }
}