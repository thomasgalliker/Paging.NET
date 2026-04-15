using FluentAssertions;
using Paging.Tests.TestData;
using System.Text.Json;
using Xunit;

namespace Paging.Tests
{
    public class PaginationSetTests
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        [Fact]
        public void ShouldInitializePaginationSetWithDefaultValues_EmptyConstructor()
        {
            // Act
            var paginationSet = new PaginationSet<Car>();

            // Assert
            paginationSet.CurrentPage.Should().Be(1);
            paginationSet.TotalPages.Should().Be(1);
            paginationSet.TotalCount.Should().Be(0);
            paginationSet.TotalCountUnfiltered.Should().Be(0);
            paginationSet.Items.Should().BeEmpty();
        }

        [Fact]
        public void ShouldInitializePaginationSetWithDefaultValues_WithDefaultParameters()
        {
            // Arrange
            PagingInfo? pagingInfo = null;
            IEnumerable<Car> items = Array.Empty<Car>();
            const int totalCount = 0;
            const int totalCountUnfiltered = 0;

            // Act
            var paginationSet = new PaginationSet<Car>(pagingInfo, items, totalCount, totalCountUnfiltered);

            // Assert
            paginationSet.CurrentPage.Should().Be(1);
            paginationSet.TotalPages.Should().Be(1);
            paginationSet.TotalCount.Should().Be(0);
            paginationSet.TotalCountUnfiltered.Should().Be(0);
            paginationSet.Items.Should().BeEmpty();
        }

        [Fact]
        public void ShouldReturnSinglePageIfCollectionIsProvided()
        {
            // Arrange
            var items = CarFactory.GenerateCarsList(3).ToList();

            // Act
            var paginationSet = new PaginationSet<Car>(items);

            // Assert
            paginationSet.CurrentPage.Should().Be(1);
            paginationSet.TotalPages.Should().Be(1);
            paginationSet.TotalCount.Should().Be(items.Count);
            paginationSet.TotalCountUnfiltered.Should().Be(items.Count);
            paginationSet.Items.Should().HaveCount(items.Count);
        }

        [Theory]
        [ClassData(typeof(SelectedPageTestData))]
        public void ShouldReturnSelectedPage(IEnumerable<Car> items, PagingInfo pagingInfo, int totalCount, int totalCountUnfiltered,
            int totalPages)
        {
            // Act
            var paginationSet = new PaginationSet<Car>(pagingInfo, items, totalCount, totalCountUnfiltered);

            // Assert
            paginationSet.CurrentPage.Should().Be(pagingInfo.CurrentPage);
            paginationSet.TotalPages.Should().Be(totalPages);
            paginationSet.TotalCount.Should().Be(totalCount);
            paginationSet.TotalCountUnfiltered.Should().Be(totalCountUnfiltered);
            paginationSet.Items.Should().HaveCount(items.Count());
        }

        public class SelectedPageTestData : TheoryData<IEnumerable<Car>, PagingInfo, int, int, int>
        {
            public SelectedPageTestData()
            {
                this.Add(CarFactory.GenerateCarsList(6), new PagingInfo { CurrentPage = 1, ItemsPerPage = 3 }, 10, 10, 4);
                this.Add(CarFactory.GenerateCarsList(6), new PagingInfo { CurrentPage = 2, ItemsPerPage = 3 }, 10, 10, 4);
                this.Add(CarFactory.GenerateCarsList(6), new PagingInfo { CurrentPage = 3, ItemsPerPage = 3 }, 10, 10, 4);
                this.Add(CarFactory.GenerateCarsList(6), new PagingInfo { CurrentPage = 4, ItemsPerPage = 3 }, 10, 10, 4);
            }
        }

        [Fact]
        public void ShouldSerializePaginationSet()
        {
            // Arrange
            var items = CarFactory.GenerateCarsList(2).ToList();
            var paginationSet = new PaginationSet<Car>(new PagingInfo { CurrentPage = 2, ItemsPerPage = 2 }, items, 5, 8);

            // Act
            var serializeObject = JsonSerializer.Serialize(paginationSet, SerializerOptions);

            // Assert
            serializeObject.Should()
                .Be(
                    "{\"currentPage\":2,\"totalPages\":3,\"totalCount\":5,\"totalCountUnfiltered\":8,\"items\":[{\"Id\":0,\"Name\":\"Car 0\"},{\"Id\":1,\"Name\":\"Car 1\"}]}");
        }

        [Fact]
        public void ShouldDeserializePaginationSet()
        {
            // Arrange
            const string serializeObject =
                "{\"currentPage\":2,\"totalPages\":3,\"totalCount\":5,\"totalCountUnfiltered\":8,\"items\":[{\"id\":1,\"name\":\"Car 1\"},{\"id\":2,\"name\":\"Car 2\"}]}";

            // Act
            var paginationSet = JsonSerializer.Deserialize<PaginationSet<Car>>(serializeObject, SerializerOptions);

            // Assert
            paginationSet.Should().NotBeNull();
            paginationSet!.CurrentPage.Should().Be(2);
            paginationSet.TotalPages.Should().Be(3);
            paginationSet.TotalCount.Should().Be(5);
            paginationSet.TotalCountUnfiltered.Should().Be(8);
            paginationSet.Items.Should().BeEquivalentTo(
                new[]
                {
                    new Car { Id = 1, Name = "Car 1" },
                    new Car { Id = 2, Name = "Car 2" }
                });
        }

        [Fact]
        public void ShouldRoundTripPaginationSet()
        {
            // Arrange
            var items = CarFactory.GenerateCarsList(2).ToList();
            var paginationSet = new PaginationSet<Car>(new PagingInfo { CurrentPage = 2, ItemsPerPage = 2 }, items, 5, 8);

            // Act
            var serializeObject = JsonSerializer.Serialize(paginationSet, SerializerOptions);
            var paginationSetResult = JsonSerializer.Deserialize<PaginationSet<Car>>(serializeObject, SerializerOptions);

            // Assert
            paginationSetResult.Should().NotBeNull();
            paginationSetResult!.CurrentPage.Should().Be(paginationSet.CurrentPage);
            paginationSetResult.TotalPages.Should().Be(paginationSet.TotalPages);
            paginationSetResult.TotalCount.Should().Be(paginationSet.TotalCount);
            paginationSetResult.TotalCountUnfiltered.Should().Be(paginationSet.TotalCountUnfiltered);
            paginationSetResult.Items.Should().BeEquivalentTo(paginationSet.Items);
        }
    }
}
