using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Paging.EF.Tests.TestData;

namespace Paging.EF.Tests
{
    /// <summary>
    /// Proves that the sort and filter expressions composed by Paging.Queryable
    /// are translated to SQL by EF Core. Unlike LINQ-to-Objects, the SQLite provider
    /// throws on untranslatable (client-evaluated) expressions.
    /// </summary>
    public class PaginationQueryableExtensionsTests : IDisposable
    {
        private readonly SqliteConnection connection;
        private readonly LicenseContext context;

        public PaginationQueryableExtensionsTests()
        {
            this.connection = new SqliteConnection("DataSource=:memory:");
            this.connection.Open();

            var options = new DbContextOptionsBuilder<LicenseContext>()
                .UseSqlite(this.connection)
                .Options;

            this.context = new LicenseContext(options);
            this.context.Database.EnsureCreated();
            Seed(this.context);
        }

        public void Dispose()
        {
            this.context.Dispose();
            this.connection.Dispose();
        }

        private static void Seed(LicenseContext context)
        {
            var anna = new Holder { Id = 1, Name = "Anna" };
            var bob = new Holder { Id = 2, Name = "Bob" };

            context.Licenses.AddRange(
                // Valid: ValidFrom <= now <= ValidTo
                new License { Id = 1, Name = "Fishing License A", ValidFrom = new DateTime(2026, 1, 1), ValidTo = new DateTime(2026, 12, 31), IsRevoked = false, Holder = anna },
                // Expired: ValidTo < now
                new License { Id = 2, Name = "Fishing License B", ValidFrom = new DateTime(2024, 1, 1), ValidTo = new DateTime(2024, 12, 31), IsRevoked = false, Holder = bob },
                // Not yet valid: ValidFrom > now
                new License { Id = 3, Name = "Hunting License", ValidFrom = new DateTime(2027, 1, 1), ValidTo = new DateTime(2027, 12, 31), IsRevoked = false, Holder = anna },
                // Revoked
                new License { Id = 4, Name = "Boating License", ValidFrom = new DateTime(2026, 1, 1), ValidTo = new DateTime(2026, 12, 31), IsRevoked = true, Holder = bob });

            context.SaveChanges();
        }

        private static PagingOptions<License> CreateLicensePagingOptions(DateTime now)
        {
            return new PagingOptions<License>(o =>
            {
                o.Property(l => l.Name).Sortable().Filterable();
                o.Property(l => l.Holder.Name).HasName("holder").Sortable().Filterable();
                o.Property(l => l.ValidFrom).Sortable().Filterable();

                // Computed sort key with captured runtime value:
                // 0 = valid, 1 = not yet valid, 2 = expired, 3 = revoked
                o.Property("validity").Sortable(l =>
                    l.IsRevoked ? 3
                    : l.ValidTo < now ? 2
                    : l.ValidFrom > now ? 1
                    : 0);

                o.Property("valid").Filterable(value => value is bool isValid
                    ? (Expression<Func<License, bool>>)(l => (!l.IsRevoked && l.ValidFrom <= now && l.ValidTo >= now) == isValid)
                    : null);

                o.DefaultSort(l => l.Id, SortOrder.Desc);

                o.Search(s => l => l.Name.ToLower().Contains(s.ToLower()) ||
                                   l.Holder.Name.ToLower().Contains(s.ToLower()));

                o.IncludeUnfilteredCount();
            });
        }

        private static readonly DateTime Now = new DateTime(2026, 6, 15);

        [Fact]
        public void ShouldSortByComputedExpression_TranslatedToSql()
        {
            // Arrange
            var pagingInfo = new PagingInfo { SortBy = "validity" };
            var pagingOptions = CreateLicensePagingOptions(Now);

            var queryable = this.context.Licenses
                .Include(l => l.Holder);

            // Act
            var paginationSet = queryable.ToPaginationSet(pagingInfo, pagingOptions);

            // Assert
            paginationSet.Items.Select(l => l.Id).Should().Equal(1, 3, 2, 4);
        }

        [Fact]
        public void ShouldSortByNestedPropertyPath_WithDefaultSortTieBreaker()
        {
            // Arrange
            var pagingInfo = new PagingInfo { SortBy = "holder" };
            var pagingOptions = CreateLicensePagingOptions(Now);

            var queryable = this.context.Licenses
                .Include(l => l.Holder);

            // Act
            var paginationSet = queryable.ToPaginationSet(pagingInfo, pagingOptions);

            // Assert: Anna's licenses first (ids 3, 1 due to Id desc tie-breaker), then Bob's (4, 2)
            paginationSet.Items.Select(l => l.Id).Should().Equal(3, 1, 4, 2);
        }

        [Fact]
        public void ShouldSortByMultipleKeys_MixingComputedAndMappedProperties()
        {
            // Arrange
            var pagingInfo = new PagingInfo { SortBy = "validity, holder desc" };
            var pagingOptions = CreateLicensePagingOptions(Now);

            var queryable = this.context.Licenses
                .Include(l => l.Holder);

            // Act
            var paginationSet = queryable.ToPaginationSet(pagingInfo, pagingOptions);

            // Assert: validity rank asc (1=valid, 3=not yet, 2=expired, 4=revoked), holder desc within equal ranks
            paginationSet.Items.Select(l => l.Id).Should().Equal(1, 3, 2, 4);
        }

        [Fact]
        public void ShouldFilterByContains_TranslatedToSql()
        {
            // Arrange
            var pagingInfo = new PagingInfo
            {
                Filter = new FilterCondition("name", FilterOperator.Contains, "fishing"),
            };
            var pagingOptions = CreateLicensePagingOptions(Now);

            var queryable = this.context.Licenses.AsQueryable();

            // Act
            var paginationSet = queryable.ToPaginationSet(pagingInfo, pagingOptions);

            // Assert
            paginationSet.Items.Select(l => l.Id).Should().Equal(2, 1);
            paginationSet.TotalCount.Should().Be(2);
            paginationSet.TotalCountUnfiltered.Should().Be(4);
        }

        [Fact]
        public void ShouldFilterByDateTimeRange_TranslatedToSql()
        {
            // Arrange
            var pagingInfo = new PagingInfo
            {
                Filter = FilterGroup.And(
                    new FilterCondition("ValidFrom", FilterOperator.GreaterThanOrEqual, "2026-01-01T00:00:00"),
                    new FilterCondition("ValidFrom", FilterOperator.LessThan, new DateTime(2027, 1, 1))),
            };
            var pagingOptions = CreateLicensePagingOptions(Now);

            var queryable = this.context.Licenses.AsQueryable();

            // Act
            var paginationSet = queryable.ToPaginationSet(pagingInfo, pagingOptions);

            // Assert
            paginationSet.Items.Select(l => l.Id).Should().Equal(4, 1);
        }

        [Fact]
        public void ShouldFilterByCustomPredicate_WithCapturedRuntimeValue()
        {
            // Arrange
            var pagingInfo = new PagingInfo
            {
                Filter = new FilterCondition("valid", FilterOperator.Equal, true),
            };
            var pagingOptions = CreateLicensePagingOptions(Now);

            var queryable = this.context.Licenses.AsQueryable();

            // Act
            var paginationSet = queryable.ToPaginationSet(pagingInfo, pagingOptions);

            // Assert
            paginationSet.Items.Select(l => l.Id).Should().Equal(1);
        }

        [Fact]
        public void ShouldSearchInNestedProperties_TranslatedToSql()
        {
            // Arrange
            var pagingInfo = new PagingInfo { Search = "anna" };
            var pagingOptions = CreateLicensePagingOptions(Now);

            var queryable = this.context.Licenses
                .Include(l => l.Holder);

            // Act
            var paginationSet = queryable.ToPaginationSet(pagingInfo, pagingOptions);

            // Assert
            paginationSet.Items.Select(l => l.Id).Should().Equal(3, 1);
            paginationSet.TotalCount.Should().Be(2);
            paginationSet.TotalCountUnfiltered.Should().Be(4);
        }

        [Fact]
        public void ShouldPageWithStableDefaultSort()
        {
            // Arrange
            var pagingInfo1 = new PagingInfo { CurrentPage = 1, ItemsPerPage = 2 };
            var pagingInfo2 = new PagingInfo { CurrentPage = 2, ItemsPerPage = 2 };
            var pagingOptions = CreateLicensePagingOptions(Now);

            var queryable = this.context.Licenses.AsQueryable();

            // Act
            var paginationSet1 = queryable.ToPaginationSet(pagingInfo1, pagingOptions);
            var paginationSet2 = queryable.ToPaginationSet(pagingInfo2, pagingOptions);

            // Assert: Id desc default sort; pages are disjoint and complete
            paginationSet1.Items.Select(l => l.Id).Should().Equal(4, 3);
            paginationSet2.Items.Select(l => l.Id).Should().Equal(2, 1);
            paginationSet1.TotalPages.Should().Be(2);
        }

        [Fact]
        public void ShouldFilterByInList_TranslatedToSql()
        {
            // Arrange
            var pagingOptions = new PagingOptions<License>(o =>
            {
                o.Property(l => l.Id).Filterable();
                o.DefaultSort(l => l.Id);
            });
            var pagingInfo = new PagingInfo
            {
                Filter = new FilterCondition("Id", FilterOperator.In, new object[] { 2, 3 }),
            };

            var queryable = this.context.Licenses.AsQueryable();

            // Act
            var paginationSet = queryable.ToPaginationSet(pagingInfo, pagingOptions);

            // Assert
            paginationSet.Items.Select(l => l.Id).Should().Equal(2, 3);
        }

        [Fact]
        public async Task ShouldPageAsync_WithStableDefaultSort()
        {
            // Arrange
            var pagingInfo = new PagingInfo { CurrentPage = 1, ItemsPerPage = 2 };
            var pagingOptions = CreateLicensePagingOptions(Now);

            var queryable = this.context.Licenses.AsQueryable();

            // Act
            var paginationSet = await queryable.ToPaginationSetAsync(pagingInfo, pagingOptions);

            // Assert: Id desc default sort
            paginationSet.Items.Select(l => l.Id).Should().Equal(4, 3);
            paginationSet.TotalCount.Should().Be(4);
            paginationSet.TotalPages.Should().Be(2);
        }

        [Fact]
        public async Task ShouldFilterAsync_TranslatedToSql()
        {
            // Arrange
            var pagingInfo = new PagingInfo
            {
                Filter = new FilterCondition("name", FilterOperator.Contains, "fishing"),
            };
            var pagingOptions = CreateLicensePagingOptions(Now);

            var queryable = this.context.Licenses.AsQueryable();

            // Act
            var paginationSet = await queryable.ToPaginationSetAsync(pagingInfo, pagingOptions);

            // Assert
            paginationSet.Items.Select(l => l.Id).Should().Equal(2, 1);
            paginationSet.TotalCount.Should().Be(2);
            paginationSet.TotalCountUnfiltered.Should().Be(4);
        }

        [Fact]
        public async Task ShouldComposeApplyPagingProjectAndPageAsync()
        {
            // Arrange: ApplyPaging -> Select (SQL projection) -> async terminal
            var pagingInfo = new PagingInfo { SortBy = "holder" };
            var pagingOptions = CreateLicensePagingOptions(Now);

            var queryable = this.context.Licenses.Include(l => l.Holder);

            // Act
            var paginationSet = await queryable
                .ApplyPaging(pagingInfo, pagingOptions)
                .Select(l => l.Id)
                .ToPaginationSetAsync(pagingInfo);

            // Assert: Anna's licenses first (3, 1 due to Id desc tie-breaker), then Bob's (4, 2)
            paginationSet.Items.Should().Equal(3, 1, 4, 2);
            paginationSet.TotalCount.Should().Be(4);
        }
    }
}
