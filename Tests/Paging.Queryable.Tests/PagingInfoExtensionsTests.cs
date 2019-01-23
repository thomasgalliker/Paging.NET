using System;
using System.Linq;
using System.Linq.Expressions;
using FluentAssertions;
using Paging.Queryable.Tests.Testdata;
using Xunit;

namespace Paging.Queryable.Tests
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
        public void ShouldCreatePaginationSet_WithSearch()
        {
            // Arrange
            var queryable = CarFactory.GenerateCarsList(20).AsQueryable();
            var pagingInfo = new PagingInfo { CurrentPage = 1, ItemsPerPage = 30, Search = "Car 1" };

            Expression<Func<Car, bool>> searchPredicate = c => c.Name.Contains(pagingInfo.Search);

            // Act
            var paginationSet = pagingInfo.CreatePaginationSet<Car, CarDto>(queryable, CarFactory.MapCarsToCarDtos, searchPredicate);

            // Assert
            paginationSet.Should().NotBeNull();
            paginationSet.Items.Should().HaveCount(11);
            paginationSet.CurrentPage.Should().Be(1);
            paginationSet.TotalPages.Should().Be(1);
            paginationSet.TotalCount.Should().Be(11);
            paginationSet.TotalCountUnfiltered.Should().Be(20);
        }

        [Fact]
        public void ShouldCreatePaginationSet_WithSearch_ItemsPerPageZero()
        {
            // Arrange
            var queryable = CarFactory.GenerateCarsList(20).AsQueryable();
            var pagingInfo = new PagingInfo { CurrentPage = 1, ItemsPerPage = 0, Search = "Car 1" };

            Expression<Func<Car, bool>> searchPredicate = c => c.Name.Contains(pagingInfo.Search);

            // Act
            var paginationSet = pagingInfo.CreatePaginationSet<Car, CarDto>(queryable, CarFactory.MapCarsToCarDtos, searchPredicate);

            // Assert
            paginationSet.Should().NotBeNull();
            paginationSet.Items.Should().HaveCount(11);
            paginationSet.CurrentPage.Should().Be(1);
            paginationSet.TotalPages.Should().Be(1);
            paginationSet.TotalCount.Should().Be(11);
            paginationSet.TotalCountUnfiltered.Should().Be(20);
        }

        [Fact]
        public void ShouldCreatePaginationSet_WithFilter_SingleProperty()
        {
            // Arrange
            var queryable = CarFactory.GenerateCarsList("BMW", "X", 3)
                .Union(CarFactory.GenerateCarsList("BMW", "M", 3))
                .Union(CarFactory.GenerateCarsList("Audi", "A", 3))
                .Union(CarFactory.GenerateCarsList("Mercedes", "G", 3))
                .AsQueryable();

            var pagingInfo = new PagingInfo { Filter = { { "Name", "bmw" } } };

            Expression<Func<Car, bool>> searchPredicate = c => c.Name.Contains(pagingInfo.Search);

            // Act
            var paginationSet = pagingInfo.CreatePaginationSet<Car, CarDto>(queryable, CarFactory.MapCarsToCarDtos, searchPredicate);

            // Assert
            paginationSet.Should().NotBeNull();
            paginationSet.Items.Should().HaveCount(6);
            paginationSet.CurrentPage.Should().Be(1);
            paginationSet.TotalPages.Should().Be(1);
            paginationSet.TotalCount.Should().Be(6);
            paginationSet.TotalCountUnfiltered.Should().Be(12);
        }

        [Fact]
        public void ShouldCreatePaginationSet_WithFilter_MultipleProperties()
        {
            // Arrange
            var queryable = CarFactory.GenerateCarsList("BMW", "X", 3)
                .Union(CarFactory.GenerateCarsList("BMW", "M", 3))
                .Union(CarFactory.GenerateCarsList("Audi", "A", 3))
                .Union(CarFactory.GenerateCarsList("Mercedes", "G", 3))
                .AsQueryable();

            var pagingInfo = new PagingInfo { Filter = { { "Name", "bmw" }, { "model", "M" } } };

            Expression<Func<Car, bool>> searchPredicate = c => c.Name.Contains(pagingInfo.Search);

            // Act
            var paginationSet = pagingInfo.CreatePaginationSet<Car, CarDto>(queryable, CarFactory.MapCarsToCarDtos, searchPredicate);

            // Assert
            paginationSet.Should().NotBeNull();
            paginationSet.Items.Should().HaveCount(3);
            paginationSet.CurrentPage.Should().Be(1);
            paginationSet.TotalPages.Should().Be(1);
            paginationSet.TotalCount.Should().Be(3);
            paginationSet.TotalCountUnfiltered.Should().Be(12);
        }

        [Fact]
        public void ShouldCreatePaginationSet_SortBy_Reverse()
        {
            // Arrange
            var queryable = CarFactory.GenerateCarsList("BMW", "X", 3)
                .Union(CarFactory.GenerateCarsList("Audi", "A", 3))
                .Union(CarFactory.GenerateCarsList("Mercedes", "G", 3))
                .AsQueryable();

            var pagingInfo = new PagingInfo { CurrentPage = 1, ItemsPerPage = 5, SortBy = "Name", Reverse = true };

            // Act
            var paginationSet = pagingInfo.CreatePaginationSet<Car, CarDto>(queryable, CarFactory.MapCarsToCarDtos);

            // Assert
            paginationSet.Should().NotBeNull();
            paginationSet.Items.Should().HaveCount(5);
            paginationSet.Items.ElementAt(0).Name.Should().Be("Mercedes 2");
            paginationSet.Items.ElementAt(1).Name.Should().Be("Mercedes 1");
            paginationSet.Items.ElementAt(2).Name.Should().Be("Mercedes 0");
            paginationSet.Items.ElementAt(3).Name.Should().Be("BMW 2");
            paginationSet.Items.ElementAt(4).Name.Should().Be("BMW 1");
            paginationSet.CurrentPage.Should().Be(1);
            paginationSet.TotalPages.Should().Be(2);
            paginationSet.TotalCount.Should().Be(9);
            paginationSet.TotalCountUnfiltered.Should().Be(9);
        }

        [Fact]
        public void ShouldCreatePaginationSet_WithMapping()
        {
            // Arrange
            var queryable = CarFactory.GenerateCarsList(10).AsQueryable();
            var pagingInfo = new PagingInfo { ItemsPerPage = 1 };

            // Act
            var paginationSet = pagingInfo.CreatePaginationSet<Car>(queryable, c => true);
            var paginationSetMapped = pagingInfo.Map<Car, CarDto>(paginationSet, CarFactory.MapCarsToCarDtos);

            // Assert
            paginationSet.Should().NotBeNull();
            paginationSetMapped.Should().NotBeNull();

            paginationSet.Items.Should().HaveCount(paginationSetMapped.Items.Count());
            paginationSet.CurrentPage.Should().Be(paginationSetMapped.CurrentPage);
            paginationSet.TotalPages.Should().Be(paginationSetMapped.TotalPages);
            paginationSet.TotalCount.Should().Be(paginationSetMapped.TotalCount);
            paginationSet.TotalCountUnfiltered.Should().Be(paginationSetMapped.TotalCountUnfiltered);
        }
    }
}
