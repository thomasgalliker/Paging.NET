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
    }
}