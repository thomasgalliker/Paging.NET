using System;
using System.Linq;
using System.Linq.Expressions;
using FluentAssertions;
using Paging.Queryable;
using Paging.Tests.Testdata;
using Xunit;

namespace Paging.Tests
{
    public class PagingInfoExtensionsTests
    {
        [Fact]
        public void ShouldCreatePaginationSet_ItemsPerPageZeroReturnsSinglePage()
        {
            // Arrange
            var queryable = CarFactory.GenerateCarsList(10).AsQueryable();
            var pagingInfo = new PagingInfo { CurrentPage = 1, ItemsPerPage = 4 };

            // Act
            var paginationSet = pagingInfo.CreatePaginationSet<Car, CarDto>(queryable, CarFactory.MapCarsToCarDtos, c => true);

            // Assert
            paginationSet.Should().NotBeNull();
            paginationSet.Items.Should().HaveCount(4);
            paginationSet.CurrentPage.Should().Be(1);
            paginationSet.TotalPages.Should().Be(3);
            paginationSet.TotalCount.Should().Be(10);
            paginationSet.TotalCountUnfiltered.Should().Be(10);
        }

        [Fact]
        public void ShouldCreatePaginationSet_ItemsPerPageDividesItemsToPages()
        {
            // Arrange
            var queryable = CarFactory.GenerateCarsList(10).AsQueryable();
            var pagingInfo = new PagingInfo { ItemsPerPage = 1 };

            // Act
            var paginationSet = pagingInfo.CreatePaginationSet<Car, CarDto>(queryable, CarFactory.MapCarsToCarDtos, c => true);

            // Assert
            paginationSet.Should().NotBeNull();
            paginationSet.Items.Should().HaveCount(1);
            paginationSet.CurrentPage.Should().Be(1);
            paginationSet.TotalPages.Should().Be(10);
            paginationSet.TotalCount.Should().Be(10);
            paginationSet.TotalCountUnfiltered.Should().Be(10);
        }

        [Fact]
        public void ShouldCreatePaginationSet_FilterItems()
        {
            // Arrange
            var queryable = CarFactory.GenerateCarsList(20).AsQueryable();
            var pagingInfo = new PagingInfo { CurrentPage = 1, ItemsPerPage = 30, Search = "Car 1" };

            Expression<Func<Car, bool>> filterPredicate = c => c.Name.Contains(pagingInfo.Search);

            // Act
            var paginationSet = pagingInfo.CreatePaginationSet<Car, CarDto>(queryable, CarFactory.MapCarsToCarDtos, filterPredicate);

            // Assert
            paginationSet.Should().NotBeNull();
            paginationSet.Items.Should().HaveCount(11);
            paginationSet.CurrentPage.Should().Be(1);
            paginationSet.TotalPages.Should().Be(1);
            paginationSet.TotalCount.Should().Be(11);
            paginationSet.TotalCountUnfiltered.Should().Be(20);
        }

        [Fact]
        public void ShouldCreatePaginationSet_FilterItems_ItemsPerPageZero()
        {
            // Arrange
            var queryable = CarFactory.GenerateCarsList(20).AsQueryable();
            var pagingInfo = new PagingInfo { CurrentPage = 1, ItemsPerPage = 0, Search = "Car 1" };

            Expression<Func<Car, bool>> filterPredicate = c => c.Name.Contains(pagingInfo.Search);

            // Act
            var paginationSet = pagingInfo.CreatePaginationSet<Car, CarDto>(queryable, CarFactory.MapCarsToCarDtos, filterPredicate);

            // Assert
            paginationSet.Should().NotBeNull();
            paginationSet.Items.Should().HaveCount(11);
            paginationSet.CurrentPage.Should().Be(1);
            paginationSet.TotalPages.Should().Be(1);
            paginationSet.TotalCount.Should().Be(11);
            paginationSet.TotalCountUnfiltered.Should().Be(20);
        }
    }
}
