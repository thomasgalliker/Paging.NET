namespace Paging.Tests
{
    public class PagingInfoTests : IDisposable
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public PagingInfoTests()
        {
            ResetDefaults();
        }

        public void Dispose()
        {
            ResetDefaults();
        }

        [Fact]
        public void PagingInfoDefault_ShouldNotBeEditable()
        {
            // Arrange
            var pagingInfo = PagingInfo.Default;

            // Act
            Action action = () => pagingInfo.CurrentPage = 99;

            // Assert
            action.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void PagingInfoDefault_ShouldCompareToDefaultInstance()
        {
            // Arrange
            var pagingInfo1 = PagingInfo.Default;
            var pagingInfo2 = new PagingInfo();

            // Act
            var areEqual = pagingInfo1 == pagingInfo2;

            // Assert
            areEqual.Should().BeTrue();
        }

        [Fact]
        public void PagingInfoDefault_ShouldReflectConfiguredDefaults()
        {
            // Arrange
            PagingInfo.DefaultItemsPerPage = 25;

            // Act
            var pagingInfo = PagingInfo.Default;

            // Assert
            pagingInfo.FirstPageIndex.Should().Be(1);
            pagingInfo.CurrentPage.Should().Be(1);
            pagingInfo.ItemsPerPage.Should().Be(25);
        }

        [Fact]
        public void ShouldEqualTo()
        {
            // Arrange
            var pagingInfo1 = new PagingInfo();
            var pagingInfo2 = new PagingInfo();

            // Act
            var hashCode = pagingInfo1.GetHashCode();
            var areEquivalent = pagingInfo2.Equals(pagingInfo1);
            var equalsOperator = pagingInfo2 == pagingInfo1;
            var notEqualsOperator = pagingInfo2 != pagingInfo1;

            // Assert
            hashCode.Should().Be(pagingInfo2.GetHashCode());
            pagingInfo2.Should().BeEquivalentTo(pagingInfo1);
            areEquivalent.Should().BeTrue();
            equalsOperator.Should().BeTrue();
            notEqualsOperator.Should().BeFalse();
        }

        [Fact]
        public void ShouldNotEqualToDefault_IfFilterIsDifferent()
        {
            // Arrange
            var pagingInfo1 = new PagingInfo { Filter = new Dictionary<string, object?>() };
            var pagingInfo2 = new PagingInfo { Filter = new Dictionary<string, object?> { { "key", "value" } } };

            // Act
            var areEqual = pagingInfo1.Equals(pagingInfo2);

            // Assert
            areEqual.Should().BeFalse();
        }

        [Fact]
        public void ShouldInitializePagingInfoWithDefaultValues()
        {
            // Act
            var pagingInfo = new PagingInfo();

            // Assert
            pagingInfo.CurrentPage.Should().Be(1);
            pagingInfo.ItemsPerPage.Should().BeNull();
            pagingInfo.Search.Should().BeNull();
            pagingInfo.Filter.Should().BeEmpty();
            pagingInfo.SortBy.Should().BeNull();
            pagingInfo.Sorting.Should().BeEmpty();
            pagingInfo.Reverse.Should().BeFalse();
        }

        [Fact]
        public void ShouldInitializePagingInfoWithConfiguredDefaultValues()
        {
            // Arrange
            PagingInfo.DefaultItemsPerPage = 10;

            // Act
            var pagingInfo = new PagingInfo();

            // Assert
            pagingInfo.FirstPageIndex.Should().Be(1);
            pagingInfo.CurrentPage.Should().Be(1);
            pagingInfo.ItemsPerPage.Should().Be(10);
        }

        [Fact]
        public void ShouldRejectInvalidFirstPageIndex()
        {
            // Act
            Action action = () => new PagingInfo { FirstPageIndex = 2 };

            // Assert
            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void ShouldRejectInvalidDefaultFirstPageIndex()
        {
            // Act
            Action action = () => PagingInfo.DefaultFirstPageIndex = 2;

            // Assert
            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void ShouldRejectCurrentPageBelowFirstPageIndex()
        {
            // Arrange
            var pagingInfo = new PagingInfo { FirstPageIndex = 1 };

            // Act
            Action action = () => pagingInfo.CurrentPage = 0;

            // Assert
            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void ShouldUpdateCurrentPageWhenFirstPageIndexChanges()
        {
            // Arrange
            var pagingInfo = new PagingInfo();

            // Act
            pagingInfo.FirstPageIndex = 0;

            // Assert
            pagingInfo.FirstPageIndex.Should().Be(0);
            pagingInfo.CurrentPage.Should().Be(0);
        }

        [Fact]
        public void ShouldRejectNegativeItemsPerPage()
        {
            // Arrange
            var pagingInfo = new PagingInfo();

            // Act
            Action action = () => pagingInfo.ItemsPerPage = -1;

            // Assert
            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void ShouldRejectNegativeDefaultItemsPerPage()
        {
            // Act
            Action action = () => PagingInfo.DefaultItemsPerPage = -1;

            // Assert
            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void ShouldSetSortByEqualToSorting_Valid()
        {
            // Arrange
            var pagingInfo = new PagingInfo();

            // Act
            pagingInfo.SortBy = "Venue.Name asc, Name desc";

            // Assert
            pagingInfo.Sorting.Should().HaveCount(2);
            pagingInfo.Sorting.Should().Contain(new Dictionary<string, SortOrder>
            {
                { "Venue.Name", SortOrder.Asc },
                { "Name", SortOrder.Desc }
            });
        }

        [Fact]
        public void ShouldSetSortByEqualToSorting_ThrowsExceptionIfInvalidSortOrder()
        {
            // Arrange
            var pagingInfo = new PagingInfo();
            pagingInfo.SortBy = "Venue.Name xxx, Name yyy";

            // Act
            Action action = () => pagingInfo.Sorting.ToList();

            // Assert
            action.Should().Throw<ArgumentException>().Which.Message.Should().Contain("Requested value 'xxx' was not found");
        }

        [Fact]
        public void ShouldSetSortingEqualToSortBy_Valid()
        {
            // Arrange
            var pagingInfo = new PagingInfo();

            // Act
            pagingInfo.Sorting = new Dictionary<string, SortOrder>
            {
                { "Venue.Name", SortOrder.Asc },
                { "Name", SortOrder.Desc }
            };

            // Assert
            pagingInfo.SortBy.Should().Be("Venue.Name Asc, Name Desc");
        }

        [Fact]
        public void ShouldSetSortingEqualToSortBy_NullSetsNull()
        {
            // Arrange
            var pagingInfo = new PagingInfo();

            // Act
            pagingInfo.Sorting = null!;

            // Assert
            pagingInfo.SortBy.Should().BeNull();
        }

        [Theory]
        [ClassData(typeof(ToStringTestData))]
        public void ShouldConvertToQueryString(PagingInfo pagingInfo, string expectedStringValue)
        {
            // Act
            var queryString = pagingInfo.ToString();

            // Assert
            queryString.Should().Be(expectedStringValue);
        }

        [Fact]
        public void ShouldConvertToQueryParameters()
        {
            // Arrange
            var pagingInfo = new PagingInfo
            {
                FirstPageIndex = 0,
                CurrentPage = 2,
                ItemsPerPage = 30,
                SortBy = "Venue.Name Asc",
                Reverse = true,
                Search = "Test value"
            };

            // Act
            var parameters = pagingInfo.ToQueryParameters();

            // Assert
            parameters.Should().Equal(new Dictionary<string, string>
            {
                { "CurrentPage", "2" },
                { "FirstPageIndex", "0" },
                { "ItemsPerPage", "30" },
                { "SortBy", "Venue.Name Asc" },
                { "Reverse", "True" },
                { "Search", "Test value" }
            });
        }

        public class ToStringTestData : TheoryData<PagingInfo, string>
        {
            public ToStringTestData()
            {
                this.Add(new PagingInfo(), "CurrentPage=1");
                this.Add(new PagingInfo { FirstPageIndex = 0, CurrentPage = 0 }, "CurrentPage=0&FirstPageIndex=0");
                this.Add(new PagingInfo { CurrentPage = 2, ItemsPerPage = 30, SortBy = "Venue.Name", Reverse = true },
                    "CurrentPage=2&ItemsPerPage=30&SortBy=Venue.Name&Reverse=True");
                this.Add(
                    new PagingInfo
                    {
                        CurrentPage = 2,
                        ItemsPerPage = 30,
                        Sorting = new Dictionary<string, SortOrder> { { "Venue.Name", SortOrder.Asc } },
                        Reverse = true
                    }, "CurrentPage=2&ItemsPerPage=30&SortBy=Venue.Name%20Asc&Reverse=True");
                this.Add(new PagingInfo { CurrentPage = 2, ItemsPerPage = 30, SortBy = "Venue.Name asc, Name asc" },
                    "CurrentPage=2&ItemsPerPage=30&SortBy=Venue.Name%20asc%2C%20Name%20asc");
                this.Add(
                    new PagingInfo
                    {
                        CurrentPage = 2,
                        ItemsPerPage = 30,
                        Sorting = new Dictionary<string, SortOrder> { { "Venue.Name", SortOrder.Asc }, { "Name", SortOrder.Asc } }
                    }, "CurrentPage=2&ItemsPerPage=30&SortBy=Venue.Name%20Asc%2C%20Name%20Asc");
                this.Add(new PagingInfo { CurrentPage = 2, ItemsPerPage = 30, Search = "Test value" },
                    "CurrentPage=2&ItemsPerPage=30&Search=Test%20value");
                this.Add(new PagingInfo { CurrentPage = 2, ItemsPerPage = 0 }, "CurrentPage=2&ItemsPerPage=0");
            }
        }

        [Fact]
        public void ShouldSerializePagingInfo()
        {
            // Arrange
            var pagingInfo = new PagingInfo
            {
                FirstPageIndex = 0,
                CurrentPage = 2,
                ItemsPerPage = 30,
                Sorting = new Dictionary<string, SortOrder> { { "Venue.Name", SortOrder.Asc }, { "Name", SortOrder.Desc } }
            };

            // Act
            var serializeObject = JsonSerializer.Serialize(pagingInfo, SerializerOptions);
            var pagingInfoResult = JsonSerializer.Deserialize<PagingInfo>(serializeObject, SerializerOptions);

            // Assert
            serializeObject.Should()
                .Be("{\"firstPageIndex\":0,\"currentPage\":2,\"itemsPerPage\":30,\"sortBy\":\"Venue.Name Asc, Name Desc\",\"sorting\":{\"Venue.Name\":0,\"Name\":1},\"reverse\":false,\"search\":null,\"filter\":{}}");
            pagingInfoResult.Should().BeEquivalentTo(pagingInfo);
        }

        [Fact]
        public void ShouldSerializeNullItemsPerPage()
        {
            // Arrange
            var pagingInfo = new PagingInfo { CurrentPage = 1, ItemsPerPage = null };

            // Act
            var serializeObject = JsonSerializer.Serialize(pagingInfo, SerializerOptions);

            // Assert
            serializeObject.Should().NotContain("firstPageIndex");
            serializeObject.Should().Contain("\"itemsPerPage\":null");
        }

        [Fact]
        public void ShouldDeserializePagingInfo_SortingFromJson()
        {
            // Arrange
            const string serializeObject =
                "{\r\n" +
                "  \"firstPageIndex\": \"1\",\r\n" +
                "  \"currentPage\": \"1\",\r\n" +
                "  \"itemsPerPage\": \"25\",\r\n" +
                "  \"sorting\": {\r\n" +
                "    \"valueDate\": \"desc\"\r\n" +
                "  },\r\n" +
                "  \"filter\": {}\r\n" +
                "}";

            // Act
            var pagingInfo = JsonSerializer.Deserialize<PagingInfo>(serializeObject, SerializerOptions)!;
            var serializeObject2 = JsonSerializer.Serialize(pagingInfo, SerializerOptions);

            // Assert
            pagingInfo.Should().NotBeNull();
            pagingInfo.SortBy.Should().Be("valueDate Desc");
            pagingInfo.Sorting.Should().Contain(new Dictionary<string, SortOrder> { { "valueDate", SortOrder.Desc } });
            serializeObject2.Should().Be(
                "{\"currentPage\":1,\"itemsPerPage\":25,\"sortBy\":\"valueDate Desc\",\"sorting\":{\"valueDate\":1},\"reverse\":false,\"search\":null,\"filter\":{}}");
        }

        [Fact]
        public void ShouldDeserializePagingInfo_SortByFromJson()
        {
            // Arrange
            const string serializeObject =
                "{\r\n  \"firstPageIndex\": \"1\",\r\n  \"currentPage\": \"1\",\r\n  \"itemsPerPage\": \"25\",\r\n  \"sortby\": \"valueDate Desc\",\r\n  \"filter\": {}\r\n}";

            // Act
            var pagingInfo = JsonSerializer.Deserialize<PagingInfo>(serializeObject, SerializerOptions)!;
            var serializeObject2 = JsonSerializer.Serialize(pagingInfo, SerializerOptions);

            // Assert
            pagingInfo.SortBy.Should().Be("valueDate Desc");
            pagingInfo.Sorting.Should().Contain(new Dictionary<string, SortOrder> { { "valueDate", SortOrder.Desc } });
            serializeObject2.Should()
                .Be("{\"currentPage\":1,\"itemsPerPage\":25,\"sortBy\":\"valueDate Desc\",\"sorting\":{\"valueDate\":1},\"reverse\":false,\"search\":null,\"filter\":{}}");
        }

        [Fact]
        public void ShouldDeserializePagingInfo_SortByAndSortingFromJson()
        {
            // Arrange
            const string serializeObject =
                "{\"FirstPageIndex\":1,\"CurrentPage\":1,\"ItemsPerPage\":25,\"SortBy\":\"valueDate Desc\",\"Sorting\":{\"valueDate\":0},\"Reverse\":false,\"Search\":null,\"Filter\":{}}";

            // Act
            var pagingInfo = JsonSerializer.Deserialize<PagingInfo>(serializeObject, SerializerOptions)!;
            var serializeObject2 = JsonSerializer.Serialize(pagingInfo, SerializerOptions);

            // Assert
            pagingInfo.SortBy.Should().Be("valueDate Desc");
            pagingInfo.Sorting.Should().Contain(new Dictionary<string, SortOrder> { { "valueDate", SortOrder.Desc } });
            serializeObject2.Should().Be(
                "{\"currentPage\":1,\"itemsPerPage\":25,\"sortBy\":\"valueDate Desc\",\"sorting\":{\"valueDate\":1},\"reverse\":false,\"search\":null,\"filter\":{}}");
        }

        [Fact]
        public void ShouldPreserveDefaultItemsPerPageWhenItemsPerPageIsMissingFromJson()
        {
            // Arrange
            PagingInfo.DefaultItemsPerPage = 15;

            const string serializeObject = "{\"filter\":{}}";

            // Act
            var pagingInfo = JsonSerializer.Deserialize<PagingInfo>(serializeObject, SerializerOptions)!;

            // Assert
            pagingInfo.FirstPageIndex.Should().Be(1);
            pagingInfo.CurrentPage.Should().Be(1);
            pagingInfo.ItemsPerPage.Should().Be(15);
        }

        private static void ResetDefaults()
        {
            PagingInfo.DefaultFirstPageIndex = 1;
            PagingInfo.DefaultItemsPerPage = null;
        }
    }
}
