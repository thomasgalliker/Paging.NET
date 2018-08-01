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
            var pagingInfoDefault = PagingInfo.Default;

            // Assert
            pagingInfoDefault.Should().BeEquivalentTo(new PagingInfo());
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
            pagingInfo.SortBy.Should().BeNull();
            pagingInfo.Reverse.Should().BeFalse();
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
                this.Add(new PagingInfo { CurrentPage = 2, ItemsPerPage = 30, SortBy = "Venue.Name asc, Name asc" }, "CurrentPage=2&ItemsPerPage=30&SortBy=Venue.Name%20asc%2C%20Name%20asc");
                this.Add(new PagingInfo { CurrentPage = 2, ItemsPerPage = 30, Search = "Test value" }, "CurrentPage=2&ItemsPerPage=30&Search=Test%20value");
            }
        }
    }
}
