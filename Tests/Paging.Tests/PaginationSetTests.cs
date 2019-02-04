using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Paging.Tests.Testdata;
using Xunit;

namespace Paging.Tests
{
    public class PaginationSetTests
    {
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
            PagingInfo pagingInfo = null;
            IEnumerable<Car> items = null;
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
        public void ShouldReturnSelectedPage(IEnumerable<Car> items, PagingInfo pagingInfo, int totalCount, int totalCountUnfiltered, int totalPages)
        {
            // Arrange

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
    }
}
