using Paging.Queryable.Tests.TestData;

namespace Paging.Queryable.Tests
{
    public class ToPaginationSetTests : IDisposable
    {
        private readonly ILogger logger;

        public ToPaginationSetTests(ITestOutputHelper testOutputHelper)
        {
            ResetDefaults();
            this.logger = new TestOutputHelperLogger<PagingInfo>(testOutputHelper);
        }

        public void Dispose()
        {
            ResetDefaults();
        }

        [Fact]
        public void ShouldCreatePaginationSet_PagingInfoIsNullReturnsDefaultPage()
        {
            // Arrange
            var queryable = CarFactory.GenerateCarsList(10).AsQueryable();
            PagingInfo? pagingInfo = null;

            // Act
            var paginationSet = queryable.ToPaginationSet(pagingInfo);

            // Assert
            paginationSet.Should().NotBeNull();
            paginationSet.Items.Should().HaveCount(10);
            paginationSet.CurrentPage.Should().Be(1);
            paginationSet.TotalPages.Should().Be(1);
            paginationSet.TotalCount.Should().Be(10);
            paginationSet.TotalCountUnfiltered.Should().BeNull();
        }

        [Fact]
        public void ShouldCreatePaginationSet_ItemsPerPagePositiveReturnsPagedItems()
        {
            // Arrange
            var queryable = CarFactory.GenerateCarsList(10).AsQueryable();
            var pagingInfo = new PagingInfo { CurrentPage = 1, ItemsPerPage = 4 };

            // Act
            var paginationSet = queryable.ToPaginationSet(pagingInfo, CreateCarDtoPagingOptions());

            // Assert
            paginationSet.Should().NotBeNull();
            paginationSet.Items.Should().HaveCount(4);
            paginationSet.CurrentPage.Should().Be(1);
            paginationSet.TotalPages.Should().Be(3);
            paginationSet.TotalCount.Should().Be(10);
            paginationSet.TotalCountUnfiltered.Should().Be(10);
        }

        [Fact]
        public void ShouldCreatePaginationSet_ItemsPerPageNullReturnsAllItems()
        {
            // Arrange
            var queryable = CarFactory.GenerateCarsList(10).AsQueryable();
            var pagingInfo = new PagingInfo { CurrentPage = 1, ItemsPerPage = null };

            // Act
            var paginationSet = queryable.ToPaginationSet(pagingInfo, CreateCarDtoPagingOptions());

            // Assert
            paginationSet.Should().NotBeNull();
            paginationSet.Items.Should().HaveCount(10);
            paginationSet.CurrentPage.Should().Be(1);
            paginationSet.TotalPages.Should().Be(1);
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
            var paginationSet = queryable.ToPaginationSet(pagingInfo, CreateCarDtoPagingOptions());

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
            var paginationSet = queryable.ToPaginationSet(pagingInfo, CreateCarDtoPagingOptions(searchPredicate));

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
            var paginationSet = queryable.ToPaginationSet(pagingInfo, CreateCarDtoPagingOptions(searchPredicate));

            // Assert
            paginationSet.Should().NotBeNull();
            paginationSet.Items.Should().BeEmpty();
            paginationSet.CurrentPage.Should().Be(1);
            paginationSet.TotalPages.Should().Be(0);
            paginationSet.TotalCount.Should().Be(11);
            paginationSet.TotalCountUnfiltered.Should().Be(20);
        }

        [Fact]
        public void ShouldCreatePaginationSet_WithZeroBasedPaging()
        {
            // Arrange
            var queryable = CarFactory.GenerateCarsList(10).AsQueryable();
            var pagingInfo = new PagingInfo { FirstPageIndex = 0, CurrentPage = 0, ItemsPerPage = 3 };

            // Act
            var paginationSet = queryable.ToPaginationSet(pagingInfo, CreateCarDtoPagingOptions());

            // Assert
            paginationSet.Should().NotBeNull();
            paginationSet.Items.Should().HaveCount(3);
            paginationSet.Items.First().ToString().Should().Be("Car Model 0, Year 2019");
            paginationSet.FirstPageIndex.Should().Be(0);
            paginationSet.CurrentPage.Should().Be(0);
            paginationSet.TotalPages.Should().Be(4);
            paginationSet.HasMorePages().Should().BeTrue();
        }

        [Fact]
        public void ShouldCreatePaginationSet_WithFilter_SingleContains()
        {
            // Arrange
            var queryable = CreateMixedBrandCars();

            var pagingInfo = new PagingInfo
            {
                Filter = new FilterCondition("Name", FilterOperator.Contains, "bmw"),
            };

            // Act
            var paginationSet = queryable.ToPaginationSet(pagingInfo, CreateCarDtoPagingOptions());

            // Assert
            paginationSet.Items.Should().HaveCount(6);
            paginationSet.TotalCount.Should().Be(6);
            paginationSet.TotalCountUnfiltered.Should().Be(12);
        }

        [Fact]
        public void ShouldCreatePaginationSet_WithFilter_SingleEqual()
        {
            // Arrange
            var queryable = CreateMixedBrandCars();

            var pagingInfo = new PagingInfo
            {
                Filter = new FilterCondition("Id", FilterOperator.Equal, 1),
            };

            // Act
            var paginationSet = queryable.ToPaginationSet(pagingInfo, CreateCarDtoPagingOptions());

            // Assert
            paginationSet.Items.Should().HaveCount(1);
            paginationSet.TotalCount.Should().Be(1);
            paginationSet.TotalCountUnfiltered.Should().Be(12);
        }

        [Fact]
        public void ShouldCreatePaginationSet_WithFilter_NotEqual()
        {
            // Arrange
            var queryable = CreateMixedBrandCars();

            var pagingInfo = new PagingInfo
            {
                Filter = new FilterCondition("Name", FilterOperator.NotEqual, "BMW"),
            };

            // Act
            var paginationSet = queryable.ToPaginationSet(pagingInfo, CreateCarDtoPagingOptions());

            // Assert: Audi (3) + Mercedes (3)
            paginationSet.Items.Should().HaveCount(6);
            paginationSet.TotalCountUnfiltered.Should().Be(12);
        }

        [Fact]
        public void ShouldCreatePaginationSet_WithFilter_MultipleProperties_And()
        {
            // Arrange
            var queryable = CarFactory.GenerateCarsList("BMW", "X", null, 2000, null, false, 3)
                .Union(CarFactory.GenerateCarsList("BMW", "X", 5000m, 2005, null, false, 3))
                .Union(CarFactory.GenerateCarsList("BMW", "X", 10000m, 2010, null, true, 3))
                .Union(CarFactory.GenerateCarsList("BMW", "X", 15000m, 2015, null, false, 3))
                .WithUniqueIds()
                .AsQueryable();

            var pagingInfo = new PagingInfo
            {
                Filter = FilterGroup.And(
                    new FilterCondition("Name", FilterOperator.Contains, "bmw"),
                    new FilterCondition("Model", FilterOperator.Contains, "x"),
                    new FilterCondition("Price", FilterOperator.Equal, 10000m),
                    new FilterCondition("Year", FilterOperator.Equal, 2010),
                    new FilterCondition("IsElectric", FilterOperator.Equal, true)),
            };

            // Act
            var paginationSet = queryable.ToPaginationSet(pagingInfo, CreateCarDtoPagingOptions());

            // Assert
            paginationSet.Items.Should().HaveCount(3);
            paginationSet.TotalCount.Should().Be(3);
            paginationSet.TotalCountUnfiltered.Should().Be(12);
        }

        [Fact]
        public void ShouldCreatePaginationSet_WithFilter_ComparisonOperators()
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
                Filter = FilterGroup.And(
                    new FilterCondition("Name", FilterOperator.Contains, "bm"),
                    new FilterCondition("Price", FilterOperator.GreaterThanOrEqual, 5000m),
                    new FilterCondition("Year", FilterOperator.LessThan, 2010)),
            };

            // Act
            var paginationSet = queryable.ToPaginationSet(pagingInfo, CreateCarDtoPagingOptions());

            // Assert: only the BMW X built in 2005 with price 5000
            paginationSet.Items.Should().HaveCount(3);
            paginationSet.Items.ElementAt(0).ToString().Should().Be("BMW X 0, Year 2005");
            paginationSet.Items.ElementAt(1).ToString().Should().Be("BMW X 1, Year 2005");
            paginationSet.Items.ElementAt(2).ToString().Should().Be("BMW X 2, Year 2005");
            paginationSet.TotalCount.Should().Be(3);
            paginationSet.TotalCountUnfiltered.Should().Be(15);
        }

        [Fact]
        public void ShouldCreatePaginationSet_WithFilter_DateTimeEqual()
        {
            // Arrange
            var queryable = CreateServiceHistoryCars();

            var pagingInfo = new PagingInfo
            {
                Filter = new FilterCondition("LastService", FilterOperator.Equal, new DateTime(2019, 1, 1, 00, 00, 00, DateTimeKind.Utc)),
            };

            // Act
            var paginationSet = queryable.ToPaginationSet(pagingInfo, CreateCarDtoPagingOptions());

            // Assert
            paginationSet.Items.Should().HaveCount(3);
            paginationSet.Items.ElementAt(0).ToString().Should().Be("BMW X 0, Year 2015");
            paginationSet.TotalCount.Should().Be(3);
            paginationSet.TotalCountUnfiltered.Should().Be(12);
        }

        [Fact]
        public void ShouldCreatePaginationSet_WithFilter_DateTimeRange_AsAndGroup()
        {
            // Arrange
            var queryable = CreateServiceHistoryCars();

            var pagingInfo = new PagingInfo
            {
                Filter = FilterGroup.And(
                    // ISO-serialized string and a typed DateTime are both accepted
                    new FilterCondition("LastService", FilterOperator.GreaterThan, "2012-01-01T00:00:00Z"),
                    new FilterCondition("LastService", FilterOperator.LessThanOrEqual, new DateTime(2019, 1, 1, 00, 00, 00, DateTimeKind.Utc))),
            };

            // Act
            var paginationSet = queryable.ToPaginationSet(pagingInfo, CreateCarDtoPagingOptions());

            // Assert
            paginationSet.Items.Should().HaveCount(3);
            paginationSet.Items.ElementAt(0).ToString().Should().Be("BMW X 0, Year 2015");
            paginationSet.TotalCount.Should().Be(3);
            paginationSet.TotalCountUnfiltered.Should().Be(12);
        }

        [Fact]
        public void ShouldCreatePaginationSet_WithFilter_In()
        {
            // Arrange
            var queryable = CreateMixedBrandCars();

            var pagingInfo = new PagingInfo
            {
                Filter = FilterGroup.And(
                    new FilterCondition("Id", FilterOperator.In, new object[] { 7, 6, 9, 10 }),
                    new FilterCondition("Name", FilterOperator.In, new[] { "Mercedes", "Audi", "non-existent" })),
            };

            // Act
            var paginationSet = queryable.ToPaginationSet(pagingInfo, CreateCarDtoPagingOptions());

            // Assert: Audi ids 6,7 and Mercedes ids 9,10
            paginationSet.Items.Should().HaveCount(4);
            paginationSet.Items.ElementAt(0).ToString().Should().Be("Audi A 0, Year 2019");
            paginationSet.Items.ElementAt(2).ToString().Should().Be("Mercedes G 0, Year 2019");
            paginationSet.TotalCount.Should().Be(4);
            paginationSet.TotalCountUnfiltered.Should().Be(12);
        }

        [Fact]
        public void ShouldCreatePaginationSet_WithFilter_OrGroup()
        {
            // Arrange
            var queryable = CreateMixedBrandCars();

            var pagingInfo = new PagingInfo
            {
                Filter = FilterGroup.Or(
                    new FilterCondition("Name", FilterOperator.Contains, "audi"),
                    new FilterCondition("Name", FilterOperator.Contains, "mercedes")),
            };

            // Act
            var paginationSet = queryable.ToPaginationSet(pagingInfo, CreateCarDtoPagingOptions());

            // Assert: Audi (3) OR Mercedes (3)
            paginationSet.Items.Should().HaveCount(6);
            paginationSet.TotalCountUnfiltered.Should().Be(12);
        }

        [Fact]
        public void ShouldCreatePaginationSet_WithFilter_NestedGroups()
        {
            // Arrange
            var queryable = CarFactory.GenerateCarsList("BMW", "X", null, 2000, 3)
                .Union(CarFactory.GenerateCarsList("BMW", "X", 5000m, 2005, 3))
                .Union(CarFactory.GenerateCarsList("Audi", "A", 15000m, 2015, 3))
                .WithUniqueIds()
                .AsQueryable();

            // (Name contains bmw AND Year < 2005) OR (Name contains audi)
            var pagingInfo = new PagingInfo
            {
                Filter = FilterGroup.Or(
                    FilterGroup.And(
                        new FilterCondition("Name", FilterOperator.Contains, "bmw"),
                        new FilterCondition("Year", FilterOperator.LessThan, 2005)),
                    new FilterCondition("Name", FilterOperator.Contains, "audi")),
            };

            // Act
            var paginationSet = queryable.ToPaginationSet(pagingInfo, CreateCarDtoPagingOptions());

            // Assert: BMW 2000 (3) + Audi (3)
            paginationSet.Items.Should().HaveCount(6);
            paginationSet.TotalCountUnfiltered.Should().Be(9);
        }

        [Fact]
        public void ShouldCreatePaginationSet_WithFilter_ParsedFromExpressionString()
        {
            // Arrange: the filter arrives as a string (e.g. from a query string) and is parsed into the tree
            var queryable = CreateMixedBrandCars();

            var pagingInfo = new PagingInfo
            {
                Filter = FilterNode.Parse("Name contains \"audi\" || (Name == \"Mercedes\" && Id >= 10)"),
            };

            // Act
            var paginationSet = queryable.ToPaginationSet(pagingInfo, CreateCarDtoPagingOptions());

            // Assert: all 3 Audi (ids 6,7,8) + Mercedes with Id >= 10 (ids 10,11)
            paginationSet.Items.Should().HaveCount(5);
            paginationSet.TotalCountUnfiltered.Should().Be(12);
        }

        [Fact]
        public void ShouldCreatePaginationSet_WithFilter_SkipsLenientlyOnUnconvertibleValue()
        {
            // Arrange
            var queryable = CreateServiceHistoryCars();

            var pagingInfo = new PagingInfo
            {
                // A non-numeric value compared to a numeric property is leniently skipped
                Filter = new FilterCondition("Price", FilterOperator.GreaterThanOrEqual, "not-a-number"),
            };

            // Act
            var paginationSet = queryable.ToPaginationSet(pagingInfo, CreateCarDtoPagingOptions());

            // Assert: filter skipped, all items returned
            paginationSet.Items.Should().HaveCount(12);
            paginationSet.TotalCount.Should().Be(12);
        }

        [Fact]
        public void ShouldThrowPagingException_ForUnknownFilterProperty()
        {
            // Arrange
            var queryable = CreateMixedBrandCars();
            var pagingOptions = new PagingOptions<Car, CarDto>(o =>
            {
                o.Property(c => c.Name).Filterable();
                o.Map(CarFactory.MapCarToCarDto);
            });

            var pagingInfo = new PagingInfo
            {
                Filter = new FilterCondition("Secret", FilterOperator.Equal, "x"),
            };

            // Act
            Action act = () => queryable.ToPaginationSet(pagingInfo, pagingOptions);

            // Assert
            act.Should().Throw<PagingException>().Which.PropertyName.Should().Be("Secret");
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
            var paginationSet1 = queryable.ToPaginationSet(pagingInfo1, CreateCarDtoPagingOptions());
            var paginationSet2 = queryable.ToPaginationSet(pagingInfo2, CreateCarDtoPagingOptions());

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
                // Unknown sort properties are ignored (UnknownPropertyHandling.Ignore), reverse has no effect
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
            var paginationSet = queryable.ToPaginationSet(pagingInfo);
            var paginationSetMapped = paginationSet.Map(CarFactory.MapCarsToCarDtos);

            // Assert
            paginationSet.Should().NotBeNull();
            paginationSetMapped.Should().NotBeNull();

            paginationSet.Items.Should().HaveCount(paginationSetMapped.Items.Count());
            paginationSet.CurrentPage.Should().Be(paginationSetMapped.CurrentPage);
            paginationSet.TotalPages.Should().Be(paginationSetMapped.TotalPages);
            paginationSet.TotalCount.Should().Be(paginationSetMapped.TotalCount);
            paginationSet.TotalCountUnfiltered.Should().Be(paginationSetMapped.TotalCountUnfiltered);
        }

        [Fact]
        public void ShouldComposeApplyPagingThenProjectThenPage()
        {
            // Arrange: exercise the composition seam (ApplyPaging -> Select -> ToPaginationSet)
            var queryable = CreateMixedBrandCars();
            var pagingOptions = CreateCarDtoPagingOptions();
            var pagingInfo = new PagingInfo
            {
                ItemsPerPage = 2,
                SortBy = "Name",
                Filter = new FilterCondition("Name", FilterOperator.Contains, "bmw"),
            };

            // Act
            var paginationSet = queryable
                .ApplyPaging(pagingInfo, pagingOptions)
                .Select(CarFactory.MapCarToCarDto)
                .ToPaginationSet(pagingInfo);

            // Assert
            paginationSet.Items.Should().HaveCount(2);
            paginationSet.TotalCount.Should().Be(6);
            // Unfiltered count is not computed on the manual seam
            paginationSet.TotalCountUnfiltered.Should().BeNull();
        }

        private static IQueryable<Car> CreateMixedBrandCars()
        {
            return CarFactory.GenerateCarsList("BMW", "X", 3)
                .Union(CarFactory.GenerateCarsList("BMW", "M", 3))
                .Union(CarFactory.GenerateCarsList("Audi", "A", 3))
                .Union(CarFactory.GenerateCarsList("Mercedes", "G", 3))
                .AsQueryable();
        }

        private static IQueryable<Car> CreateServiceHistoryCars()
        {
            return CarFactory.GenerateCarsList("BMW", "X", null, 2000, null, false, 3)
                .Union(CarFactory.GenerateCarsList("BMW", "X", 5000m, 2005, null, false, 3))
                .Union(CarFactory.GenerateCarsList("BMW", "X", 10000m, 2010, new DateTime(2012, 1, 1, 00, 00, 00, DateTimeKind.Utc), false, 3))
                .Union(CarFactory.GenerateCarsList("BMW", "X", 15000m, 2015, new DateTime(2019, 1, 1, 00, 00, 00, DateTimeKind.Utc), false, 3))
                .AsQueryable();
        }

        /// <summary>
        /// Creates paging options exposing the common car properties for sorting and filtering,
        /// mapping the queried cars to <see cref="CarDto"/> and optionally applying a search predicate.
        /// Unknown property names are ignored to keep the sort theory data permissive.
        /// </summary>
        private static PagingOptions<Car, CarDto> CreateCarDtoPagingOptions(Expression<Func<Car, bool>>? searchPredicate = null)
        {
            return new PagingOptions<Car, CarDto>(o =>
            {
                o.Property(c => c.Id).Sortable().Filterable();
                o.Property(c => c.Name).Sortable().Filterable();
                o.Property(c => c.Model).Sortable().Filterable();
                o.Property(c => c.Price).Sortable().Filterable();
                o.Property(c => c.Year).Sortable().Filterable();
                o.Property(c => c.IsElectric).Sortable().Filterable();
                o.Property(c => c.LastService).Sortable().Filterable();
                o.Property(c => c.LastOilChange).Sortable().Filterable();

                o.UnknownProperties(UnknownPropertyHandling.Ignore);
                o.IncludeUnfilteredCount();

                if (searchPredicate != null)
                {
                    o.Search(_ => searchPredicate);
                }

                o.Map(CarFactory.MapCarToCarDto);
            });
        }

        private static void ResetDefaults()
        {
            PagingInfo.DefaultFirstPageIndex = 1;
            PagingInfo.DefaultItemsPerPage = null;
        }
    }
}
