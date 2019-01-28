using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
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

            // Assert
            pagingInfoDefault.Should().BeEquivalentTo(pagingInfo);
            pagingInfoDefault.Equals(pagingInfo).Should().BeTrue();
            (pagingInfoDefault == pagingInfo).Should().BeTrue();
            (pagingInfoDefault != pagingInfo).Should().BeFalse();
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
            pagingInfo.Sorting.Should().BeEmpty();
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
    }
}
