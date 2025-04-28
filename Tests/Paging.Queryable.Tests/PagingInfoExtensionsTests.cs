using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Paging.Queryable.Tests.Logging;
using Paging.Queryable.Tests.Testdata;
using Xunit;
using Xunit.Abstractions;

namespace Paging.Queryable.Tests
{
    public class PagingInfoExtensionsTests
    {
        private readonly ILogger logger;

        public PagingInfoExtensionsTests(ITestOutputHelper testOutputHelper)
        {
            this.logger = new TestOutputHelperLogger<PagingInfo>(testOutputHelper);
        }

        [Fact]
        public void ShouldCreatePaginationSet_PagingInfoIsNullReturnsDefaultPage()
        {
            // Arrange
            var queryable = CarFactory.GenerateCarsList(10).AsQueryable();
            PagingInfo pagingInfo = null;

            // Act
            var paginationSet = pagingInfo.CreatePaginationSet(queryable);

            // Assert
            paginationSet.Should().NotBeNull();
            paginationSet.Items.Should().HaveCount(10);
            paginationSet.CurrentPage.Should().Be(1);
            paginationSet.TotalPages.Should().Be(1);
            paginationSet.TotalCount.Should().Be(10);
            paginationSet.TotalCountUnfiltered.Should().Be(10);
        }

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
        public void ShouldCreatePaginationSet_WithFilter_SingleString()
        {
            // Arrange
            var queryable = CarFactory.GenerateCarsList("BMW", "X", 3)
                .Union(CarFactory.GenerateCarsList("BMW", "M", 3))
                .Union(CarFactory.GenerateCarsList("Audi", "A", 3))
                .Union(CarFactory.GenerateCarsList("Mercedes", "G", 3))
                .AsQueryable();

            var pagingInfo = new PagingInfo
            {
                Filter =
                {
                    {
                        "Name", "bmw"
                    }
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
            paginationSet.TotalCountUnfiltered.Should().Be(12);
        }

        [Fact]
        public void ShouldCreatePaginationSet_WithFilter_SingleInt()
        {
            // Arrange
            var queryable = CarFactory.GenerateCarsList("BMW", "X", 3)
                .Union(CarFactory.GenerateCarsList("BMW", "M", 3))
                .Union(CarFactory.GenerateCarsList("Audi", "A", 3))
                .Union(CarFactory.GenerateCarsList("Mercedes", "G", 3))
                .AsQueryable();

            var pagingInfo = new PagingInfo
            {
                Filter =
                {
                    { "id", 1 }
                }
            };

            // Act
            var paginationSet = pagingInfo.CreatePaginationSet<Car, CarDto>(queryable, CarFactory.MapCarsToCarDtos);

            // Assert
            paginationSet.Should().NotBeNull();
            paginationSet.Items.Should().HaveCount(1);
            paginationSet.CurrentPage.Should().Be(1);
            paginationSet.TotalPages.Should().Be(1);
            paginationSet.TotalCount.Should().Be(1);
            paginationSet.TotalCountUnfiltered.Should().Be(12);
        }

        [Fact]
        public void ShouldCreatePaginationSet_WithFilter_MultipleProperties()
        {
            // Arrange
            var queryable = CarFactory.GenerateCarsList("BMW", "X", null, 2000, null, false, 3)
                .Union(CarFactory.GenerateCarsList("BMW", "X", 5000m, 2005, null, false, 3))
                .Union(CarFactory.GenerateCarsList("BMW", "X", 10000m, 2010, null, true, 3))
                .Union(CarFactory.GenerateCarsList("BMW", "X", 15000m, 2015, null, false, 3))
                .WithUnitqueIds()
                .AsQueryable();

            var pagingInfo = new PagingInfo
            {
                Filter = new Dictionary<string, object>
                {
                    {"Name", "bmw"},
                    {"model", "x"},
                    {"Price", 10000m},
                    {"year", 2010},
                    {"isElectric", true}
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
                    {"Price", "5000"} // Matches Prices with string equivalents of "5000" and "15000"
                }
            };

            // Act
            var paginationSet = pagingInfo.CreatePaginationSet<Car, CarDto>(queryable, CarFactory.MapCarsToCarDtos);

            // Assert
            paginationSet.Should().NotBeNull();
            paginationSet.Items.Should().HaveCount(6);
            paginationSet.Items.ElementAt(0).ToString().Should().Be("BMW X 0, Year 2005");
            paginationSet.Items.ElementAt(1).ToString().Should().Be("BMW X 1, Year 2005");
            paginationSet.Items.ElementAt(2).ToString().Should().Be("BMW X 2, Year 2005");
            paginationSet.Items.ElementAt(3).ToString().Should().Be("BMW X 0, Year 2015");
            paginationSet.Items.ElementAt(4).ToString().Should().Be("BMW X 1, Year 2015");
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
                .Union(CarFactory.GenerateCarsList("Audi", "X", 15000m, 2015, 3))
                .AsQueryable();

            var pagingInfo = new PagingInfo
            {
                Filter = new Dictionary<string, object>
                {
                    {"Name", "bm"},
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
            paginationSet.TotalCountUnfiltered.Should().Be(15);
        }

        [Fact]
        public void ShouldCreatePaginationSet_WithFilter_WithDateTime()
        {
            // Arrange
            var queryable = CarFactory.GenerateCarsList("BMW", "X", null, 2000, null, false, 3)
                .Union(CarFactory.GenerateCarsList("BMW", "X", 5000m, 2005, null, false, 3))
                .Union(CarFactory.GenerateCarsList("BMW", "X", 10000m, 2010, new DateTime(2012, 1, 1, 00, 00, 00, DateTimeKind.Utc), false, 3))
                .Union(CarFactory.GenerateCarsList("BMW", "X", 15000m, 2015, new DateTime(2019, 1, 1, 00, 00, 00, DateTimeKind.Utc), false, 3))
                .AsQueryable();


            var pagingInfo = new PagingInfo
            {
                Filter = new Dictionary<string, object>
                {
                    {
                        "LastService", new DateTime(2019, 1, 1, 00, 00, 00, DateTimeKind.Utc)
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
        public void ShouldCreatePaginationSet_WithFilter_WithDateTimeRanges()
        {
            // Arrange
            var queryable = CarFactory.GenerateCarsList("BMW", "X", null, 2000, null, false, 3)
                .Union(CarFactory.GenerateCarsList("BMW", "X", 5000m, 2005, null, false, 3))
                .Union(CarFactory.GenerateCarsList("BMW", "X", 10000m, 2010, new DateTime(2012, 1, 1, 00, 00, 00, DateTimeKind.Utc), false, 3))
                .Union(CarFactory.GenerateCarsList("BMW", "X", 15000m, 2015, new DateTime(2019, 1, 1, 00, 00, 00, DateTimeKind.Utc), false, 3))
                .AsQueryable();


            var pagingInfo = new PagingInfo
            {
                Filter = new Dictionary<string, object>
                {
                    {
                        "LastService", new Dictionary<string, object>
                        {
                            {">", "2012-01-01T00:00:00Z" }, // DateTime can be an ISO-serialized string
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
        public void ShouldCreatePaginationSet_WithFilter_WithDateTimeOffsetRanges()
        {
            // Arrange
            var queryable = CarFactory.GenerateCarsList("BMW", "X", null, 2000, null, false, 3)
                .Union(CarFactory.GenerateCarsList("BMW", "X", 5000m, 2005, null, false, 3))
                .Union(CarFactory.GenerateCarsList("BMW", "X", 10000m, 2010, new DateTime(2012, 1, 1, 00, 00, 00, DateTimeKind.Utc), false, 3))
                .Union(CarFactory.GenerateCarsList("BMW", "X", 15000m, 2015, new DateTime(2019, 1, 1, 00, 00, 00, DateTimeKind.Utc), false, 3))
                .AsQueryable();

            var pagingInfo = new PagingInfo
            {
                Filter = new Dictionary<string, object>
                {
                    {
                        "LastOilChange", new Dictionary<string, object>
                        {
                            {">", new DateTimeOffset(2012, 1, 1, 00, 00, 00, TimeSpan.Zero) },
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
        public void ShouldCreatePaginationSet_WithFilter_WithOrFilterValues()
        {
            // Arrange
            var queryable = CarFactory.GenerateCarsList("BMW", "X", 3)
                .Union(CarFactory.GenerateCarsList("BMW", "M", 3))
                .Union(CarFactory.GenerateCarsList("Audi", "A", 3))
                .Union(CarFactory.GenerateCarsList("Mercedes", "G", 3))
                .AsQueryable();

            var pagingInfo = new PagingInfo
            {
                Filter = new Dictionary<string, object>
                {
                    { "Id", new object[]{ 7, 6, 9, 10 }},
                    { "Name", new []
                        {
                            "Mercedes", // Exact match
                            "Audi", // Exact match
                            "bmw", // Wrong case
                            "non-existent" // Invalid name
                        }
                    },
                    { "Year", new int[]{}},
                    { "Price", new []{ "wrong-type" }} // Type mismatch
                }
            };

            // Act
            var paginationSet = pagingInfo.CreatePaginationSet<Car, CarDto>(queryable, CarFactory.MapCarsToCarDtos);

            // Assert
            paginationSet.Should().NotBeNull();
            paginationSet.Items.Should().HaveCount(4);
            paginationSet.Items.ElementAt(0).ToString().Should().Be("Audi A 0, Year 2019");
            paginationSet.Items.ElementAt(1).ToString().Should().Be("Audi A 1, Year 2019");
            paginationSet.Items.ElementAt(2).ToString().Should().Be("Mercedes G 0, Year 2019");
            paginationSet.Items.ElementAt(3).ToString().Should().Be("Mercedes G 1, Year 2019");
            paginationSet.CurrentPage.Should().Be(1);
            paginationSet.TotalPages.Should().Be(1);
            paginationSet.TotalCount.Should().Be(4);
            paginationSet.TotalCountUnfiltered.Should().Be(12);
        }

        [Fact]
        public void ShouldCreatePaginationSet_WithFilter_WithInvalidRangeKey()
        {
            // Arrange
            var queryable = CarFactory.GenerateCarsList("BMW", "X", null, 2000, null, false, 3)
                .Union(CarFactory.GenerateCarsList("BMW", "X", 5000m, 2005, null, false, 3))
                .Union(CarFactory.GenerateCarsList("BMW", "X", 10000m, 2010, new DateTime(2012, 1, 1, 00, 00, 00, DateTimeKind.Utc), false, 3))
                .Union(CarFactory.GenerateCarsList("BMW", "X", 15000m, 2015, new DateTime(2019, 1, 1, 00, 00, 00, DateTimeKind.Utc), false, 3))
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
            var queryable = CarFactory.GenerateCarsList("BMW", "X", null, 2000, null, false, 3)
                .Union(CarFactory.GenerateCarsList("BMW", "X", 5000m, 2005, null, false, 3))
                .Union(CarFactory.GenerateCarsList("BMW", "X", 10000m, 2010, new DateTime(2012, 1, 1, 00, 00, 00, DateTimeKind.Utc), false, 3))
                .Union(CarFactory.GenerateCarsList("BMW", "X", 15000m, 2015, new DateTime(2019, 1, 1, 00, 00, 00, DateTimeKind.Utc), false, 3))
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

        [Theory]
        [ClassData(typeof(SortByTestData))]
        public void ShouldCreatePaginationSet_SortBy(string sortBy, bool reverse, ICollection<string> expectedItemStrings1, ICollection<string> expectedItemStrings2)
        {
            // Arrange
            var queryable = CarFactory.GenerateCarsList("BMW", "X", 3)
                .Union(CarFactory.GenerateCarsList("Audi", "A", 3))
                .Union(CarFactory.GenerateCarsList("Mercedes", "G", 3))
                .AsQueryable();

            var pagingInfo1 = new PagingInfo { CurrentPage = 1, ItemsPerPage = 5, SortBy = sortBy, Reverse = reverse };
            var pagingInfo2 = new PagingInfo { CurrentPage = 2, ItemsPerPage = 5, SortBy = sortBy, Reverse = reverse };

            // Act
            var paginationSet1 = pagingInfo1.CreatePaginationSet<Car, CarDto>(queryable, CarFactory.MapCarsToCarDtos);
            var paginationSet2 = pagingInfo2.CreatePaginationSet<Car, CarDto>(queryable, CarFactory.MapCarsToCarDtos);

            // Assert
            paginationSet1.Should().NotBeNull();
            paginationSet2.Should().NotBeNull();

            paginationSet1.Items.Should().HaveCount(expectedItemStrings1.Count);
            paginationSet2.Items.Should().HaveCount(expectedItemStrings2.Count);

            paginationSet1.Items.Select(i => i.ToString()).Should().ContainInOrder(expectedItemStrings1);
            paginationSet2.Items.Select(i => i.ToString()).Should().ContainInOrder(expectedItemStrings2);

            paginationSet1.CurrentPage.Should().Be(1);
            paginationSet1.TotalPages.Should().Be(2);
            paginationSet1.TotalCount.Should().Be(9);
            paginationSet1.TotalCountUnfiltered.Should().Be(9);

            paginationSet2.CurrentPage.Should().Be(2);
            paginationSet2.TotalPages.Should().Be(2);
            paginationSet2.TotalCount.Should().Be(9);
            paginationSet2.TotalCountUnfiltered.Should().Be(9);
        }

        /// <summary>
        /// SortBy -> Expected list of {cars, page 1} and  {cars, page 2}
        /// </summary>
        public class SortByTestData : TheoryData<string, bool, ICollection<string>, ICollection<string>>
        {
            public SortByTestData()
            {
                // sortBy: "Model", reverse: false
                this.Add("Model", false, new[]
                {
                    "Audi A 0, Year 2019",
                    "Audi A 1, Year 2019",
                    "Audi A 2, Year 2019",
                    "Mercedes G 0, Year 2019",
                    "Mercedes G 1, Year 2019"
                }, new[]
                {
                    "Mercedes G 2, Year 2019",
                    "BMW X 0, Year 2019",
                    "BMW X 1, Year 2019",
                    "BMW X 2, Year 2019"
                });

                // sortBy: "Model", reverse: true
                this.Add("Model", true, new[]
                {
                    "BMW X 2, Year 2019",
                    "BMW X 1, Year 2019",
                    "BMW X 0, Year 2019",
                    "Mercedes G 2, Year 2019",
                    "Mercedes G 1, Year 2019"
                }, new[]
                {
                    "Mercedes G 0, Year 2019",
                    "Audi A 2, Year 2019",
                    "Audi A 1, Year 2019",
                    "Audi A 0, Year 2019"
                });

                // sortBy: "Model asc", reverse: false
                this.Add("Model asc", false, new[]
                {
                    "Audi A 0, Year 2019",
                    "Audi A 1, Year 2019",
                    "Audi A 2, Year 2019",
                    "Mercedes G 0, Year 2019",
                    "Mercedes G 1, Year 2019"
                }, new[]
                {
                    "Mercedes G 2, Year 2019",
                    "BMW X 0, Year 2019",
                    "BMW X 1, Year 2019",
                    "BMW X 2, Year 2019"
                });

                // sortBy: "Model desc", reverse: false
                this.Add("Model desc", false, new[]
                {
                    "BMW X 2, Year 2019",
                    "BMW X 1, Year 2019",
                    "BMW X 0, Year 2019",
                    "Mercedes G 2, Year 2019",
                    "Mercedes G 1, Year 2019"
                }, new[]
                {
                    "Mercedes G 0, Year 2019",
                    "Audi A 2, Year 2019",
                    "Audi A 1, Year 2019",
                    "Audi A 0, Year 2019"
                });

                // sortBy: "Name, Model", reverse: false
                this.Add("Name, Model", false, new[]
                {
                    "Audi A 0, Year 2019",
                    "Audi A 1, Year 2019",
                    "Audi A 2, Year 2019",
                    "BMW X 0, Year 2019",
                    "BMW X 1, Year 2019"
                }, new[]
                {
                    "BMW X 2, Year 2019",
                    "Mercedes G 0, Year 2019",
                    "Mercedes G 1, Year 2019",
                    "Mercedes G 2, Year 2019"
                });

                // sortBy: "Name, Model", reverse: true
                this.Add("Name, Model", true, new[]
                {
                    "Mercedes G 2, Year 2019",
                    "Mercedes G 1, Year 2019",
                    "Mercedes G 0, Year 2019",
                    "BMW X 2, Year 2019",
                    "BMW X 1, Year 2019"
                }, new[]
                {
                    "BMW X 0, Year 2019",
                    "Audi A 2, Year 2019",
                    "Audi A 1, Year 2019",
                    "Audi A 0, Year 2019"
                });

                // sortBy: "Name desc, Model asc", reverse: false
                this.Add("Name desc, Model asc", false, new[]
                {
                    "Mercedes G 0, Year 2019",
                    "Mercedes G 1, Year 2019",
                    "Mercedes G 2, Year 2019",
                    "BMW X 0, Year 2019",
                    "BMW X 1, Year 2019"
                }, new[]
                {
                    "BMW X 2, Year 2019",
                    "Audi A 0, Year 2019",
                    "Audi A 1, Year 2019",
                    "Audi A 2, Year 2019"
                });

                // sortBy: "Name desc, Model asc", reverse: true
                this.Add("Name desc, Model asc", true, new[]
                {
                    "Audi A 2, Year 2019",
                    "Audi A 1, Year 2019",
                    "Audi A 0, Year 2019",
                    "BMW X 2, Year 2019",
                    "BMW X 1, Year 2019"
                }, new[]
                {
                    "BMW X 0, Year 2019",
                    "Mercedes G 2, Year 2019",
                    "Mercedes G 1, Year 2019",
                    "Mercedes G 0, Year 2019"
                });

                // sortBy: "nonexistent", reverse: false
                // Non-existent properties are not sorted, also, reverse has no effect
                this.Add("nonexistent", false, new[]
                {
                    "BMW X 0, Year 2019",
                    "BMW X 1, Year 2019",
                    "BMW X 2, Year 2019",
                    "Audi A 0, Year 2019",
                    "Audi A 1, Year 2019"
                }, new[]
                {
                    "Audi A 2, Year 2019",
                    "Mercedes G 0, Year 2019",
                    "Mercedes G 1, Year 2019",
                    "Mercedes G 2, Year 2019"
                });

                // sortBy: "", reverse: true
                // Non-existent properties are not sorted, also, reverse has no effect
                this.Add("", true, new[]
                {
                    "BMW X 0, Year 2019",
                    "BMW X 1, Year 2019",
                    "BMW X 2, Year 2019",
                    "Audi A 0, Year 2019",
                    "Audi A 1, Year 2019"
                }, new[]
                {
                    "Audi A 2, Year 2019",
                    "Mercedes G 0, Year 2019",
                    "Mercedes G 1, Year 2019",
                    "Mercedes G 2, Year 2019"
                });
            }
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
