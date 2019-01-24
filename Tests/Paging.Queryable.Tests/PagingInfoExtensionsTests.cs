using System;
using System.Collections.Generic;
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
            var pagingInfo = new PagingInfo { CurrentPage = 1, ItemsPerPage = 30, Search = "Model 1" };

            Expression<Func<Car, bool>> searchPredicate = c => c.Model.Contains(pagingInfo.Search);

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
            var pagingInfo = new PagingInfo { CurrentPage = 1, ItemsPerPage = 0, Search = "Model 1" };

            Expression<Func<Car, bool>> searchPredicate = c => c.Model.Contains(pagingInfo.Search);

            // Act
            var paginationSet =
                pagingInfo.CreatePaginationSet<Car, CarDto>(queryable, CarFactory.MapCarsToCarDtos, searchPredicate);

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

            // Act
            var paginationSet = pagingInfo.CreatePaginationSet<Car, CarDto>(queryable, CarFactory.MapCarsToCarDtos);

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
            var queryable = CarFactory.GenerateCarsList("BMW", "X", null, 2000, 3)
                .Union(CarFactory.GenerateCarsList("BMW", "X", 5000m, 2005, 3))
                .Union(CarFactory.GenerateCarsList("BMW", "X", 10000m, 2010, 3))
                .Union(CarFactory.GenerateCarsList("BMW", "X", 15000m, 2015, 3))
                .AsQueryable();

            var pagingInfo = new PagingInfo
            {
                Filter = new Dictionary<string, object>
                {
                    {"Name", "bmw"},
                    {"model", "x"},
                    {"Price", 10000m},
                    {"year", 2010}
                }
            };

            // Act
            var paginationSet = pagingInfo.CreatePaginationSet<Car, CarDto>(queryable, CarFactory.MapCarsToCarDtos);

            // Assert
            paginationSet.Should().NotBeNull();
            paginationSet.Items.Should().HaveCount(3);
            paginationSet.CurrentPage.Should().Be(1);
            paginationSet.TotalPages.Should().Be(1);
            paginationSet.TotalCount.Should().Be(3);
            paginationSet.TotalCountUnfiltered.Should().Be(12);
        }

        [Fact]
        public void ShouldCreatePaginationSet_WithFilter_SkipNullValues()
        {
            // Arrange
            var queryable = CarFactory.GenerateCarsList(null, "X", null, 2000, 3)
                .Union(CarFactory.GenerateCarsList("BMW", "X", 5000m, 2005, 3))
                .AsQueryable();

            var pagingInfo = new PagingInfo
            {
                Filter = new Dictionary<string, object>
                {
                    {"price", null}
                }
            };

            // Act
            var paginationSet = pagingInfo.CreatePaginationSet<Car, CarDto>(queryable, CarFactory.MapCarsToCarDtos);

            // Assert
            paginationSet.Should().NotBeNull();
            paginationSet.Items.Should().HaveCount(6);
            paginationSet.CurrentPage.Should().Be(1);
            paginationSet.TotalPages.Should().Be(1);
            paginationSet.TotalCount.Should().Be(6);
            paginationSet.TotalCountUnfiltered.Should().Be(6);
        }

        [Fact]
        public void ShouldCreatePaginationSet_WithFilter_WithStringNumbers()
        {
            // Arrange
            var queryable = CarFactory.GenerateCarsList("BMW", "X", null, 2000, 3)
                .Union(CarFactory.GenerateCarsList("BMW", "X", 5000m, 2005, 3))
                .Union(CarFactory.GenerateCarsList("BMW", "X", 10000m, 2010, 3))
                .Union(CarFactory.GenerateCarsList("BMW", "X", 15000m, 2015, 3))
                .AsQueryable();

            var pagingInfo = new PagingInfo
            {
                Filter = new Dictionary<string, object>
                {
                    {"Price", "5000"},
                }
            };

            // Act
            var paginationSet = pagingInfo.CreatePaginationSet<Car, CarDto>(queryable, CarFactory.MapCarsToCarDtos);

            // Assert
            paginationSet.Should().NotBeNull();
            paginationSet.Items.Should().HaveCount(6);
            paginationSet.Items.ElementAt(0).ToString().Should().Be("BMW X 0, Year 2005");
            paginationSet.Items.ElementAt(1).ToString().Should().Be("BMW X 0, Year 2015");
            paginationSet.Items.ElementAt(2).ToString().Should().Be("BMW X 1, Year 2005");
            paginationSet.Items.ElementAt(3).ToString().Should().Be("BMW X 1, Year 2015");
            paginationSet.Items.ElementAt(4).ToString().Should().Be("BMW X 2, Year 2005");
            paginationSet.Items.ElementAt(5).ToString().Should().Be("BMW X 2, Year 2015");
            paginationSet.CurrentPage.Should().Be(1);
            paginationSet.TotalPages.Should().Be(1);
            paginationSet.TotalCount.Should().Be(6);
            paginationSet.TotalCountUnfiltered.Should().Be(12);
        }

        [Fact]
        public void ShouldCreatePaginationSet_WithFilter_WithNumberRanges()
        {
            // Arrange
            var queryable = CarFactory.GenerateCarsList("BMW", "X", null, 2000, 3)
                .Union(CarFactory.GenerateCarsList("BMW", "X", 5000m, 2005, 3))
                .Union(CarFactory.GenerateCarsList("BMW", "X", 10000m, 2010, 3))
                .Union(CarFactory.GenerateCarsList("BMW", "X", 15000m, 2015, 3))
                .AsQueryable();

            var pagingInfo = new PagingInfo
            {
                Filter = new Dictionary<string, object>
                {
                    {"Name", "bmw"},
                    {"model", ""},
                    {"Price", ">=5000"},
                    {"year", "<2010"}
                }
            };

            // Act
            var paginationSet = pagingInfo.CreatePaginationSet<Car, CarDto>(queryable, CarFactory.MapCarsToCarDtos);

            // Assert
            paginationSet.Should().NotBeNull();
            paginationSet.Items.Should().HaveCount(3);
            paginationSet.Items.ElementAt(0).ToString().Should().Be("BMW X 0, Year 2005");
            paginationSet.Items.ElementAt(1).ToString().Should().Be("BMW X 1, Year 2005");
            paginationSet.Items.ElementAt(2).ToString().Should().Be("BMW X 2, Year 2005");
            paginationSet.CurrentPage.Should().Be(1);
            paginationSet.TotalPages.Should().Be(1);
            paginationSet.TotalCount.Should().Be(3);
            paginationSet.TotalCountUnfiltered.Should().Be(12);
        }

        [Fact]
        public void ShouldCreatePaginationSet_WithFilter_WithDateTimeRanges()
        {
            // Arrange
            var queryable = CarFactory.GenerateCarsList("BMW", "X", null, 2000, null, 3)
                .Union(CarFactory.GenerateCarsList("BMW", "X", 5000m, 2005, null, 3))
                .Union(CarFactory.GenerateCarsList("BMW", "X", 10000m, 2010, new DateTime(2012, 1, 1, 00, 00, 00, DateTimeKind.Utc), 3))
                .Union(CarFactory.GenerateCarsList("BMW", "X", 15000m, 2015, new DateTime(2019, 1, 1, 00, 00, 00, DateTimeKind.Utc), 3))
                .AsQueryable();


            var pagingInfo = new PagingInfo
            {
                Filter = new Dictionary<string, object>
                {
                    {
                        "LastService", new Dictionary<string, object>
                        {
                            {">", "2012-01-01T00:00:00Z" },
                            {"<=", new DateTime(2019, 1, 1, 00, 00, 00, DateTimeKind.Utc) },
                        }
                    }
                }
            };

            // Act
            var paginationSet = pagingInfo.CreatePaginationSet<Car, CarDto>(queryable, CarFactory.MapCarsToCarDtos);

            // Assert
            paginationSet.Should().NotBeNull();
            paginationSet.Items.Should().HaveCount(3);
            paginationSet.Items.ElementAt(0).ToString().Should().Be("BMW X 0, Year 2015");
            paginationSet.Items.ElementAt(1).ToString().Should().Be("BMW X 1, Year 2015");
            paginationSet.Items.ElementAt(2).ToString().Should().Be("BMW X 2, Year 2015");
            paginationSet.CurrentPage.Should().Be(1);
            paginationSet.TotalPages.Should().Be(1);
            paginationSet.TotalCount.Should().Be(3);
            paginationSet.TotalCountUnfiltered.Should().Be(12);
        }

        [Fact]
        public void ShouldCreatePaginationSet_WithFilter_WithInvalidRangeKey()
        {
            // Arrange
            var queryable = CarFactory.GenerateCarsList("BMW", "X", null, 2000, null, 3)
                .Union(CarFactory.GenerateCarsList("BMW", "X", 5000m, 2005, null, 3))
                .Union(CarFactory.GenerateCarsList("BMW", "X", 10000m, 2010, new DateTime(2012, 1, 1, 00, 00, 00, DateTimeKind.Utc), 3))
                .Union(CarFactory.GenerateCarsList("BMW", "X", 15000m, 2015, new DateTime(2019, 1, 1, 00, 00, 00, DateTimeKind.Utc), 3))
                .AsQueryable();

            var pagingInfo = new PagingInfo
            {
                Filter = new Dictionary<string, object>
                {
                    {
                        "LastService", new Dictionary<string, object>
                        {
                            {"", null }
                        }
                    }
                }
            };

            // Act
            var paginationSet = pagingInfo.CreatePaginationSet<Car, CarDto>(queryable, CarFactory.MapCarsToCarDtos);

            // Assert
            paginationSet.Should().NotBeNull();
            paginationSet.Items.Should().HaveCount(12);
            paginationSet.CurrentPage.Should().Be(1);
            paginationSet.TotalPages.Should().Be(1);
            paginationSet.TotalCount.Should().Be(12);
            paginationSet.TotalCountUnfiltered.Should().Be(12);
        }

        [Fact]
        public void ShouldCreatePaginationSet_WithFilter_WithInvalidRangeValue()
        {
            // Arrange
            var queryable = CarFactory.GenerateCarsList("BMW", "X", null, 2000, null, 3)
                .Union(CarFactory.GenerateCarsList("BMW", "X", 5000m, 2005, null, 3))
                .Union(CarFactory.GenerateCarsList("BMW", "X", 10000m, 2010, new DateTime(2012, 1, 1, 00, 00, 00, DateTimeKind.Utc), 3))
                .Union(CarFactory.GenerateCarsList("BMW", "X", 15000m, 2015, new DateTime(2019, 1, 1, 00, 00, 00, DateTimeKind.Utc), 3))
                .AsQueryable();

            var pagingInfo = new PagingInfo
            {
                Filter = new Dictionary<string, object>
                {
                    { "Year", "> "},
                    {
                        "LastService", new Dictionary<string, object>
                        {
                            {">", new object() },
                        }
                    }
                }
            };

            // Act
            var paginationSet = pagingInfo.CreatePaginationSet<Car, CarDto>(queryable, CarFactory.MapCarsToCarDtos);

            // Assert
            paginationSet.Should().NotBeNull();
            paginationSet.Items.Should().HaveCount(12);
            paginationSet.CurrentPage.Should().Be(1);
            paginationSet.TotalPages.Should().Be(1);
            paginationSet.TotalCount.Should().Be(12);
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

            var pagingInfo = new PagingInfo { CurrentPage = 1, ItemsPerPage = 5, SortBy = "Model", Reverse = true };

            // Act
            var paginationSet = pagingInfo.CreatePaginationSet<Car, CarDto>(queryable, CarFactory.MapCarsToCarDtos);

            // Assert
            paginationSet.Should().NotBeNull();
            paginationSet.Items.Should().HaveCount(5);
            paginationSet.Items.ElementAt(0).ToString().Should().Be("BMW X 2, Year 2019");
            paginationSet.Items.ElementAt(1).ToString().Should().Be("BMW X 1, Year 2019");
            paginationSet.Items.ElementAt(2).ToString().Should().Be("BMW X 0, Year 2019");
            paginationSet.Items.ElementAt(3).ToString().Should().Be("Mercedes G 2, Year 2019");
            paginationSet.Items.ElementAt(4).ToString().Should().Be("Mercedes G 1, Year 2019");
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
