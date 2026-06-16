using Moq;
using Paging.Queryable.Tests.TestData;

namespace Paging.Queryable.Tests
{
    public class PagingOptionsTests
    {
        [Fact]
        public void ShouldSortByRegisteredProperty_ExternalNameInferred_CaseInsensitive()
        {
            // Arrange
            var carsQueryable = CreateCarsQueryable();
            var pagingOptions = new PagingOptions<Car>(o => o.Property(c => c.Name).Sortable());
            var pagingInfo = new PagingInfo { SortBy = "name" };

            // Act
            var paginationSet = carsQueryable.ToPaginationSet(pagingInfo, pagingOptions);

            // Assert
            paginationSet.Items.Select(c => c.Name).Should().Equal("Audi", "BMW", "Tesla");
        }

        [Fact]
        public void ShouldSortByMappedExternalName()
        {
            // Arrange
            var carsQueryable = CreateCarsQueryable();
            var pagingOptions = new PagingOptions<Car>(o => o.Property(c => c.Model).HasName("Brand").Sortable());
            var pagingInfo = new PagingInfo { SortBy = "Brand desc" };

            // Act
            var paginationSet = carsQueryable.ToPaginationSet(pagingInfo, pagingOptions);

            // Assert
            paginationSet.Items.Select(c => c.Model).Should().Equal("X5", "Model 3", "A4");
        }

        [Fact]
        public void ShouldSortByNestedPropertyPath()
        {
            // Arrange
            var carsQueryable = CreateCarsQueryable();
            var pagingOptions = new PagingOptions<Car>(o => o.Property(c => c.Owner!.Name).HasName("owner").Sortable());
            var pagingInfo = new PagingInfo { SortBy = "owner" };

            // Act
            var paginationSet = carsQueryable.ToPaginationSet(pagingInfo, pagingOptions);

            // Assert
            paginationSet.Items.Select(c => c.Id).Should().Equal(2, 3, 1);
        }

        [Fact]
        public void ShouldThrowPagingException_WhenSortPropertyNotRegistered()
        {
            // Arrange
            var carsQueryable = CreateCarsQueryable();
            var pagingOptions = new PagingOptions<Car>(o => o.Property(c => c.Name).Sortable());
            var pagingInfo = new PagingInfo { SortBy = "Price" };

            // Act
            Action action = () => carsQueryable.ToPaginationSet(pagingInfo, pagingOptions);

            // Assert
            action.Should().Throw<PagingException>()
                .Which.PropertyName.Should().Be("Price");
        }

        [Fact]
        public void ShouldIgnoreUnknownSortProperty_AndApplyDefaultSort()
        {
            // Arrange
            var carsQueryable = CreateCarsQueryable();
            var pagingOptions = new PagingOptions<Car>(o =>
            {
                o.UnknownSortProperties(UnknownPropertyHandling.Ignore);
                o.DefaultSort(c => c.Id, SortOrder.Desc);
            });
            var pagingInfo = new PagingInfo { SortBy = "Price" };

            // Act
            var paginationSet = carsQueryable.ToPaginationSet(pagingInfo, pagingOptions);

            // Assert
            paginationSet.Items.Select(c => c.Id).Should().Equal(3, 2, 1);
        }

        [Fact]
        public void ShouldSortByComputedExpression()
        {
            // Arrange
            var carsQueryable = CreateCarsQueryable();
            var pagingOptions = new PagingOptions<Car>(o =>
            {
                o.Property("Rank").Sortable(c => c.IsElectric ? 0 : (c.Year < 2015 ? 2 : 1));
            });
            var pagingInfo = new PagingInfo { SortBy = "rank" };

            // Act
            var paginationSet = carsQueryable.ToPaginationSet(pagingInfo, pagingOptions);

            // Assert
            paginationSet.Items.Select(c => c.Id).Should().Equal(3, 2, 1);
        }

        [Fact]
        public void ShouldSortByComputedExpression_Reversed()
        {
            // Arrange
            var carsQueryable = CreateCarsQueryable();
            var pagingOptions = new PagingOptions<Car>(o =>
            {
                o.Property("Rank").Sortable(c => c.IsElectric ? 0 : (c.Year < 2015 ? 2 : 1));
            });
            var pagingInfo = new PagingInfo { SortBy = "Rank", Reverse = true };

            // Act
            var paginationSet = carsQueryable.ToPaginationSet(pagingInfo, pagingOptions);

            // Assert
            paginationSet.Items.Select(c => c.Id).Should().Equal(1, 2, 3);
        }

        [Fact]
        public void ShouldEvaluateSortKeyFactory_OnEveryQuery()
        {
            // Arrange
            var carsQueryable = CreateCarsQueryable();
            var negate = false;
            var pagingOptions = new PagingOptions<Car>(o =>
            {
                o.Property("year").Sortable(() => negate
                    ? (Expression<Func<Car, int>>)(c => -c.Year)
                    : c => c.Year);
            });
            var pagingInfo = new PagingInfo { SortBy = "year" };

            // Act
            var paginationSet1 = carsQueryable.ToPaginationSet(pagingInfo, pagingOptions);
            negate = true;
            var paginationSet2 = carsQueryable.ToPaginationSet(pagingInfo, pagingOptions);

            // Assert
            paginationSet1.Items.Select(c => c.Id).Should().Equal(1, 2, 3);
            paginationSet2.Items.Select(c => c.Id).Should().Equal(3, 2, 1);
        }

        [Fact]
        public void ShouldApplyDefaultSort_AsPrimarySortOrder_NotAffectedByReverse()
        {
            // Arrange
            var carsQueryable = CreateCarsQueryable();
            var pagingOptions = new PagingOptions<Car>(o => o.DefaultSort(c => c.Id, SortOrder.Desc));
            var pagingInfo = new PagingInfo { Reverse = true };

            // Act
            var paginationSet = carsQueryable.ToPaginationSet(pagingInfo, pagingOptions);

            // Assert
            paginationSet.Items.Select(c => c.Id).Should().Equal(3, 2, 1);
        }

        [Fact]
        public void ShouldApplyDefaultSort_AsTieBreaker()
        {
            // Arrange
            var carsQueryable = new[]
            {
                new Car { Id = 1, Name = "BMW" },
                new Car { Id = 2, Name = "BMW" },
                new Car { Id = 3, Name = "Audi" },
            }.AsQueryable();

            var pagingOptions = new PagingOptions<Car>(o =>
            {
                o.Property(c => c.Name).Sortable();
                o.DefaultSort(c => c.Id, SortOrder.Desc);
            });
            var pagingInfo = new PagingInfo { SortBy = "Name" };

            // Act
            var paginationSet = carsQueryable.ToPaginationSet(pagingInfo, pagingOptions);

            // Assert
            paginationSet.Items.Select(c => c.Id).Should().Equal(3, 2, 1);
        }

        [Fact]
        public void ShouldApplyDefaultSort_WhenPagingInfoIsNull()
        {
            // Arrange
            var carsQueryable = CreateCarsQueryable();
            var pagingOptions = new PagingOptions<Car>(o => o.DefaultSort(c => c.Id, SortOrder.Desc));

            // Act
            var paginationSet = carsQueryable.ToPaginationSet(null, pagingOptions);

            // Assert
            paginationSet.Items.Select(c => c.Id).Should().Equal(3, 2, 1);
        }

        [Fact]
        public void ShouldFilterByMappedExternalName()
        {
            // Arrange
            var carsQueryable = CreateCarsQueryable();
            var pagingOptions = new PagingOptions<Car>(o =>
            {
                o.Property(c => c.Model).HasName("Brand").Filterable();
                o.IncludeUnfilteredCount();
            });
            var pagingInfo = new PagingInfo
            {
                Filter = new FilterCondition("brand", FilterOperator.Contains, "model"),
            };

            // Act
            var paginationSet = carsQueryable.ToPaginationSet(pagingInfo, pagingOptions);

            // Assert
            paginationSet.Items.Select(c => c.Id).Should().Equal(3);
            paginationSet.TotalCount.Should().Be(1);
            paginationSet.TotalCountUnfiltered.Should().Be(3);
        }

        [Fact]
        public void ShouldThrowPagingException_WhenFilterPropertyNotRegistered()
        {
            // Arrange
            var carsQueryable = CreateCarsQueryable();
            var pagingOptions = new PagingOptions<Car>(o => o.Property(c => c.Name).Sortable());
            var pagingInfo = new PagingInfo
            {
                Filter = new FilterCondition("Price", FilterOperator.Equal, 30000m),
            };

            // Act
            Action action = () => carsQueryable.ToPaginationSet(pagingInfo, pagingOptions);

            // Assert
            action.Should().Throw<PagingException>()
                .Which.PropertyName.Should().Be("Price");
        }

        [Fact]
        public void ShouldIgnoreUnknownFilterProperty()
        {
            // Arrange
            var carsQueryable = CreateCarsQueryable();
            var pagingOptions = new PagingOptions<Car>(o => o.UnknownFilterProperties(UnknownPropertyHandling.Ignore));
            var pagingInfo = new PagingInfo
            {
                Filter = new FilterCondition("Price", FilterOperator.Equal, 30000m),
            };

            // Act
            var paginationSet = carsQueryable.ToPaginationSet(pagingInfo, pagingOptions);

            // Assert
            paginationSet.Items.Should().HaveCount(3);
        }

        [Fact]
        public void ShouldConfigureUnknownHandling_SeparatelyForSortAndFilter()
        {
            // Arrange
            var carsQueryable = CreateCarsQueryable();
            var pagingOptions = new PagingOptions<Car>(o =>
            {
                o.Property(c => c.Price).Filterable();
                o.UnknownSortProperties(UnknownPropertyHandling.Ignore);
                o.UnknownFilterProperties(UnknownPropertyHandling.Throw);
            });
            var pagingInfo = new PagingInfo
            {
                // Unknown sort property is ignored, while the registered filter property is applied
                SortBy = "Unknown",
                Filter = new FilterCondition("Price", FilterOperator.GreaterThanOrEqual, 40000m),
            };

            // Act
            var paginationSet = carsQueryable.ToPaginationSet(pagingInfo, pagingOptions);

            // Assert
            paginationSet.Items.Should().HaveCount(2);
        }

        [Fact]
        public void ShouldFilterByCustomPredicate()
        {
            // Arrange
            var carsQueryable = CreateCarsQueryable();
            var pagingOptions = new PagingOptions<Car>(o =>
            {
                o.Property("electric").Filterable(value => value is bool isElectric
                    ? (isElectric ? (Expression<Func<Car, bool>>)(c => c.IsElectric) : c => !c.IsElectric)
                    : null);
            });
            var pagingInfo = new PagingInfo
            {
                Filter = new FilterCondition("Electric", FilterOperator.Equal, true),
            };

            // Act
            var paginationSet = carsQueryable.ToPaginationSet(pagingInfo, pagingOptions);

            // Assert
            paginationSet.Items.Select(c => c.Id).Should().Equal(3);
        }

        [Fact]
        public void ShouldSkipCustomFilterPredicate_WhenFactoryReturnsNull()
        {
            // Arrange
            var carsQueryable = CreateCarsQueryable();
            var pagingOptions = new PagingOptions<Car>(o =>
            {
                o.Property("electric").Filterable(value => value is bool isElectric
                    ? (Expression<Func<Car, bool>>)(c => c.IsElectric == isElectric)
                    : null);
            });
            var pagingInfo = new PagingInfo
            {
                Filter = new FilterCondition("electric", FilterOperator.Equal, "not-a-bool"),
            };

            // Act
            var paginationSet = carsQueryable.ToPaginationSet(pagingInfo, pagingOptions);

            // Assert
            paginationSet.Items.Should().HaveCount(3);
        }

        [Fact]
        public void ShouldRestrictPropertyToSorting()
        {
            // Arrange
            var carsQueryable = CreateCarsQueryable();
            var pagingOptions = new PagingOptions<Car>(o =>
            {
                o.Property(c => c.Year).Sortable();
            });
            var sortPagingInfo = new PagingInfo { SortBy = "Year desc" };
            var filterPagingInfo = new PagingInfo
            {
                Filter = new FilterCondition("Year", FilterOperator.Equal, 2010),
            };

            // Act
            var sortedPaginationSet = carsQueryable.ToPaginationSet(sortPagingInfo, pagingOptions);
            Action filterAction = () => carsQueryable.ToPaginationSet(filterPagingInfo, pagingOptions);

            // Assert
            sortedPaginationSet.Items.Select(c => c.Id).Should().Equal(3, 2, 1);
            filterAction.Should().Throw<PagingException>()
                .Which.PropertyName.Should().Be("Year");
        }

        [Fact]
        public void ShouldRestrictPropertyToFiltering()
        {
            // Arrange
            var carsQueryable = CreateCarsQueryable();
            var pagingOptions = new PagingOptions<Car>(o =>
            {
                o.Property(c => c.Year).Filterable();
            });
            var sortPagingInfo = new PagingInfo { SortBy = "Year" };
            var filterPagingInfo = new PagingInfo
            {
                Filter = new FilterCondition("Year", FilterOperator.Equal, 2010),
            };

            // Act
            Action sortAction = () => carsQueryable.ToPaginationSet(sortPagingInfo, pagingOptions);
            var filteredPaginationSet = carsQueryable.ToPaginationSet(filterPagingInfo, pagingOptions);

            // Assert
            sortAction.Should().Throw<PagingException>().Which.PropertyName.Should().Be("Year");
            filteredPaginationSet.Items.Select(c => c.Id).Should().Equal(1);
        }

        [Fact]
        public void ShouldSortByCustomKey_AndFilterByPropertyPath_OnSameProperty()
        {
            // Arrange
            var carsQueryable = CreateCarsQueryable();
            var pagingOptions = new PagingOptions<Car>(o =>
            {
                o.Property(c => c.Year)
                    .Sortable(c => c.Year < 2015 ? 1 : 0)   // custom sort key...
                    .Filterable();                          // ...while filtering uses the Year property
            });
            var sortPagingInfo = new PagingInfo { SortBy = "Year" };
            var filterPagingInfo = new PagingInfo
            {
                Filter = new FilterCondition("Year", FilterOperator.Equal, 2010),
            };

            // Act
            var sortedPaginationSet = carsQueryable.ToPaginationSet(sortPagingInfo, pagingOptions);
            var filteredPaginationSet = carsQueryable.ToPaginationSet(filterPagingInfo, pagingOptions);

            // Assert
            sortedPaginationSet.Items.Select(c => c.Id).Should().Equal(2, 3, 1);
            filteredPaginationSet.Items.Select(c => c.Id).Should().Equal(1);
        }

        [Fact]
        public void ShouldThrowInvalidOperationException_WhenSortableIsDeclaredTwice()
        {
            // Act
            Action action = () => new PagingOptions<Car>(o =>
            {
                o.Property(c => c.Year).Sortable().Sortable(c => c.Year);
            });

            // Assert
            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*already declared as sortable*");
        }

        [Fact]
        public void ShouldThrowInvalidOperationException_WhenNoCapabilityIsDeclared()
        {
            // Arrange
            var carsQueryable = CreateCarsQueryable();
            var pagingOptions = new PagingOptions<Car>(o => o.Property(c => c.Year));

            // Act
            Action action = () => carsQueryable.ToPaginationSet(new PagingInfo(), pagingOptions);

            // Assert
            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*declares no capabilities*");
        }

        [Fact]
        public void ShouldThrowArgumentException_WhenComputedExpressionIsRegisteredAsProperty()
        {
            // Act
            Action action = () => new PagingOptions<Car>(o => o.Property(c => c.Year + 1));

            // Assert
            action.Should().Throw<ArgumentException>()
                .WithMessage("*property access chain*");
        }

        [Fact]
        public void ShouldThrowArgumentException_WhenExternalNameIsRegisteredTwice()
        {
            // Act
            Action action = () => new PagingOptions<Car>(o =>
            {
                o.Property(c => c.Name).Sortable();
                o.Property("name").Sortable(c => c.Name);
            });

            // Assert
            action.Should().Throw<ArgumentException>()
                .WithMessage("*'name'*already registered*");
        }

        [Fact]
        public void ShouldThrowPagingException_WhenRegisteredPropertyPathIsInvalid()
        {
            // Arrange
            var carsQueryable = CreateCarsQueryable();
            var pagingOptions = new PagingOptions<Car>(o => o.Property("Owner.Nonexistent").Sortable());

            // Act
            Action action = () => carsQueryable.ToPaginationSet(new PagingInfo(), pagingOptions);

            // Assert
            action.Should().Throw<PagingException>()
                .WithMessage("*Owner.Nonexistent*");
        }

        [Fact]
        public void ShouldApplySearchPredicate_OnlyWhenSearchTextIsNotEmpty()
        {
            // Arrange
            var carsQueryable = CreateCarsQueryable();
            var searchInvoked = false;
            var pagingOptions = new PagingOptions<Car>(o =>
            {
                o.Search(s =>
                {
                    searchInvoked = true;
                    return c => c.Name != null && c.Name.ToLower().Contains(s.ToLower());
                });
                o.IncludeUnfilteredCount();
            });

            // Act
            var emptySearchPaginationSet = carsQueryable.ToPaginationSet(new PagingInfo { Search = "" }, pagingOptions);
            var searchInvokedAfterEmptySearch = searchInvoked;
            var searchPaginationSet = carsQueryable.ToPaginationSet(new PagingInfo { Search = "bmw" }, pagingOptions);

            // Assert
            searchInvokedAfterEmptySearch.Should().BeFalse();
            searchInvoked.Should().BeTrue();
            emptySearchPaginationSet.Items.Should().HaveCount(3);
            searchPaginationSet.Items.Select(c => c.Id).Should().Equal(1);
            searchPaginationSet.TotalCount.Should().Be(1);
            searchPaginationSet.TotalCountUnfiltered.Should().Be(3);
        }

        [Fact]
        public void ShouldMatchExternalNamesCaseSensitive_WithOrdinalNameComparer()
        {
            // Arrange
            var carsQueryable = CreateCarsQueryable();
            var pagingOptions = new PagingOptions<Car>(o =>
            {
                o.NameComparer(StringComparer.Ordinal);
                o.Property(c => c.Model).HasName("Brand").Sortable();
            });

            // Act
            var paginationSet = carsQueryable.ToPaginationSet(new PagingInfo { SortBy = "Brand" }, pagingOptions);
            Action action = () => carsQueryable.ToPaginationSet(new PagingInfo { SortBy = "brand" }, pagingOptions);

            // Assert
            paginationSet.Items.Select(c => c.Model).Should().Equal("A4", "Model 3", "X5");
            action.Should().Throw<PagingException>()
                .Which.PropertyName.Should().Be("brand");
        }

        [Fact]
        public void ShouldFreezeOnFirstUse_AndRemainReusable()
        {
            // Arrange
            var carsQueryable = CreateCarsQueryable();
            var pagingOptions = new PagingOptions<Car>(o => o.Property(c => c.Name).Sortable());
            var pagingInfo = new PagingInfo { SortBy = "Name" };

            // Act
            var paginationSet1 = carsQueryable.ToPaginationSet(pagingInfo, pagingOptions);
            Action configureAction = () => pagingOptions.Property(c => c.Model);
            var paginationSet2 = carsQueryable.ToPaginationSet(pagingInfo, pagingOptions);

            // Assert
            configureAction.Should().Throw<InvalidOperationException>();
            paginationSet1.Items.Select(c => c.Id).Should().Equal(2, 1, 3);
            paginationSet2.Items.Select(c => c.Id).Should().Equal(2, 1, 3);
        }

        [Fact]
        public void ShouldThrowInvalidOperationException_WhenMapIsNotConfigured()
        {
            // Arrange
            var carsQueryable = CreateCarsQueryable();
            var pagingOptions = new PagingOptions<Car, CarDto>(o => o.Property(c => c.Name).Sortable());

            // Act
            Action action = () => carsQueryable.ToPaginationSet(new PagingInfo(), pagingOptions);

            // Assert
            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*Map(...)*");
        }

        [Fact]
        public void ShouldUseClassBasedPagingOptions()
        {
            // Arrange
            var carsQueryable = CreateCarsQueryable();

            var dateTimeMock = new Mock<IDateTime>();
            dateTimeMock.SetupGet(d => d.UtcNow)
                .Returns(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            var pagingOptions = new CarPagingOptions(dateTimeMock.Object);
            var pagingInfo = new PagingInfo { SortBy = "Age", Search = "BMW" };

            // Act
            var paginationSet = carsQueryable.ToPaginationSet(pagingInfo, pagingOptions);

            // Assert
            paginationSet.Should().NotBeNull();
            paginationSet.Items.Should().AllBeOfType<CarDto>();
            paginationSet.Items.Select(c => c.Name).Should().Equal("BMW");
            paginationSet.TotalCount.Should().Be(1);
            paginationSet.TotalCountUnfiltered.Should().Be(3);
        }

        [Fact]
        public void ShouldSortByComputedAge_WithClassBasedPagingOptions()
        {
            // Arrange
            var carsQueryable = CreateCarsQueryable();

            var dateTimeMock = new Mock<IDateTime>();
            dateTimeMock.SetupGet(d => d.UtcNow)
                .Returns(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            var pagingOptions = new CarPagingOptions(dateTimeMock.Object);
            var pagingInfo = new PagingInfo { SortBy = "Age desc" };

            // Act
            var paginationSet = carsQueryable.ToPaginationSet(pagingInfo, pagingOptions);

            // Assert
            paginationSet.Items.Select(c => c.Id).Should().Equal(1, 2, 3);
        }

        private static IQueryable<Car> CreateCarsQueryable()
        {
            return new[]
            {
                new Car { Id = 1, Name = "BMW", Model = "X5", Year = 2010, Price = 30000m, IsElectric = false, Owner = new CarOwner { Name = "Charlie" } },
                new Car { Id = 2, Name = "Audi", Model = "A4", Year = 2020, Price = 40000m, IsElectric = false, Owner = new CarOwner { Name = "Anna" } },
                new Car { Id = 3, Name = "Tesla", Model = "Model 3", Year = 2022, Price = 50000m, IsElectric = true, Owner = new CarOwner { Name = "Berta" } },
            }.AsQueryable();
        }
    }
}
