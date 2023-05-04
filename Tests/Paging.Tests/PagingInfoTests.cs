using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace Paging.Tests
{
    public class PagingInfoTests
    {
        [Fact]
        public void ShouldEqualToDefault()
        {
            // Arrange
            var pagingInfo = new PagingInfo();

            // Act
            var pagingInfoDefault = PagingInfo.Default;

            // Assert GetHashCode
            var hashCodeHashCode = pagingInfo.GetHashCode();
            var pagingInfoDefaultHashCode = pagingInfoDefault.GetHashCode();
            hashCodeHashCode.Should().Be(pagingInfoDefaultHashCode);

            // Assert Equals
            pagingInfoDefault.Should().BeEquivalentTo(pagingInfo);
            pagingInfoDefault.Equals(pagingInfo).Should().BeTrue();

            // Assert Operators
            (pagingInfoDefault == pagingInfo).Should().BeTrue();
            (pagingInfoDefault != pagingInfo).Should().BeFalse();
        }

        [Fact]
        public void ShouldNotEqualToDefault_IfFilterIsDifferent()
        {
            // Arrange
            var pagingInfo1 = new PagingInfo { Filter = null };
            var pagingInfo2 = new PagingInfo { Filter = new Dictionary<string, object> { { "key", "value" } } };

            // Act
            var equal = pagingInfo1.Equals(pagingInfo2);

            // Assert
            equal.Should().BeFalse();
        }

        [Fact]
        public void ShouldInitializePagingInfoWithDefaultValues()
        {
            // Arrange
            var pagingInfo = new PagingInfo();

            // Assert
            pagingInfo.CurrentPage.Should().Be(1);
            pagingInfo.ItemsPerPage.Should().Be(0);
            pagingInfo.Search.Should().BeNull();
            pagingInfo.Filter.Should().BeEmpty();
            pagingInfo.SortBy.Should().BeNull();
            pagingInfo.Sorting.Should().BeNull();
            pagingInfo.Reverse.Should().BeFalse();
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
            pagingInfo.Sorting = null;

            // Assert
            pagingInfo.SortBy.Should().BeNull();
        }

        [Theory]
        [ClassData(typeof(ToStringTestData))]
        public void ShouldConvertToQueryString(PagingInfo pagingInfo, string expectedStringValue)
        {
            // Act
            var stringValue = pagingInfo.ToString();

            // Assert
            stringValue.Should().Be(expectedStringValue);
        }

        public class ToStringTestData : TheoryData<PagingInfo, string>
        {
            public ToStringTestData()
            {
                this.Add(new PagingInfo(), "CurrentPage=1&ItemsPerPage=0");
                this.Add(new PagingInfo { CurrentPage = 2, ItemsPerPage = 30, SortBy = "Venue.Name", Reverse = true }, "CurrentPage=2&ItemsPerPage=30&SortBy=Venue.Name&Reverse=True");
                this.Add(new PagingInfo { CurrentPage = 2, ItemsPerPage = 30, Sorting = new Dictionary<string, SortOrder> { { "Venue.Name", SortOrder.Asc } }, Reverse = true }, "CurrentPage=2&ItemsPerPage=30&SortBy=Venue.Name%20Asc&Reverse=True");
                this.Add(new PagingInfo { CurrentPage = 2, ItemsPerPage = 30, SortBy = "Venue.Name asc, Name asc" }, "CurrentPage=2&ItemsPerPage=30&SortBy=Venue.Name%20asc%2C%20Name%20asc");
                this.Add(new PagingInfo { CurrentPage = 2, ItemsPerPage = 30, Sorting = new Dictionary<string, SortOrder> { { "Venue.Name", SortOrder.Asc }, { "Name", SortOrder.Asc } } }, "CurrentPage=2&ItemsPerPage=30&SortBy=Venue.Name%20Asc%2C%20Name%20Asc");
                this.Add(new PagingInfo { CurrentPage = 2, ItemsPerPage = 30, Search = "Test value" }, "CurrentPage=2&ItemsPerPage=30&Search=Test%20value");
            }
        }

        [Fact]
        public void ShouldSerializePagingInfo()
        {
            // Arrange
            var pagingInfo = new PagingInfo
            {
                CurrentPage = 2,
                ItemsPerPage = 30,
                //SortBy = "Venue.Name asc, Name desc"
                Sorting = new Dictionary<string, SortOrder>
                {
                    { "Venue.Name", SortOrder.Asc },
                    { "Name", SortOrder.Desc }
                }
            };

            // Act
            var serializeObject = JsonConvert.SerializeObject(pagingInfo);
            var pagingInfoResult = JsonConvert.DeserializeObject<PagingInfo>(serializeObject);

            // Assert
            pagingInfoResult.Should().BeEquivalentTo(pagingInfo);
        }

        [Fact]
        public void ShouldDeserializePagingInfo_SortingFromJson()
        {
            // Arrange
            const string serializeObject = "{\r\n  \"currentPage\": \"1\",\r\n  \"itemsPerPage\": \"25\",\r\n  \"sorting\": {\r\n    \"valueDate\": \"desc\"\r\n  },\r\n  \"filter\": {}\r\n}";

            // Act
            var pagingInfo = JsonConvert.DeserializeObject<PagingInfo>(serializeObject);

            // Assert
            pagingInfo.SortBy.Should().Be("valueDate Desc");
            pagingInfo.Sorting.Should().Contain(new Dictionary<string, SortOrder>
            {
                { "valueDate", SortOrder.Desc }
            });

            // Bug: Serialization of PagingInfo generates SortBy and Sorting properties to JSON, this is not ideal:
            var serializeObject2 = JsonConvert.SerializeObject(pagingInfo);
            serializeObject2.Should().Be("{\"CurrentPage\":1,\"ItemsPerPage\":25,\"SortBy\":\"valueDate Desc\",\"Sorting\":{\"valueDate\":1},\"Reverse\":false,\"Search\":null,\"Filter\":{}}");
        }

        [Fact]
        public void ShouldDeserializePagingInfo_SortByFromJson()
        {
            // Arrange
            const string serializeObject = "{\r\n  \"currentPage\": \"1\",\r\n  \"itemsPerPage\": \"25\",\r\n  \"sortby\": \"valueDate Desc\",\r\n  \"filter\": {}\r\n}";

            // Act
            var pagingInfo = JsonConvert.DeserializeObject<PagingInfo>(serializeObject);

            // Assert
            pagingInfo.SortBy.Should().Be("valueDate Desc");
            pagingInfo.Sorting.Should().Contain(new Dictionary<string, SortOrder>
            {
                { "valueDate", SortOrder.Desc }
            });

            // Bug: Serialization of PagingInfo generates SortBy and Sorting properties to JSON, this is not ideal:
            var serializeObject2 = JsonConvert.SerializeObject(pagingInfo);
            serializeObject2.Should().Be("{\"CurrentPage\":1,\"ItemsPerPage\":25,\"SortBy\":\"valueDate Desc\",\"Sorting\":{\"valueDate\":1},\"Reverse\":false,\"Search\":null,\"Filter\":{}}");
        }

        [Fact]
        public void ShouldDeserializePagingInfo_SortByAndSortingFromJson()
        {
            // Arrange
            // Hint: If both given, SortBy and Sorting, only SortBy has effect
            const string serializeObject = "{\"CurrentPage\":1,\"ItemsPerPage\":25,\"SortBy\":\"valueDate Desc\",\"Sorting\":{\"valueDate\":0},\"Reverse\":false,\"Search\":null,\"Filter\":{}}";

            // Act
            var pagingInfo = JsonConvert.DeserializeObject<PagingInfo>(serializeObject);

            // Assert
            pagingInfo.SortBy.Should().Be("valueDate Desc");
            pagingInfo.Sorting.Should().Contain(new Dictionary<string, SortOrder>
            {
                { "valueDate", SortOrder.Desc }
            });

            // Bug: Serialization of PagingInfo generates SortBy and Sorting properties to JSON, this is not ideal:
            var serializeObject2 = JsonConvert.SerializeObject(pagingInfo);
            serializeObject2.Should().Be("{\"CurrentPage\":1,\"ItemsPerPage\":25,\"SortBy\":\"valueDate Desc\",\"Sorting\":{\"valueDate\":1},\"Reverse\":false,\"Search\":null,\"Filter\":{}}");
        }
    }
}
