using FluentAssertions;
using Paging.Tests.Testdata;
using Xunit;

namespace Paging.Tests
{
    public class PaginationSetTests
    {
        [Fact]
        public void ShouldInitializePaginationSetWithDefaultValues()
        {
            // Arrange
            var paginationSet = new PaginationSet<Car>();

            // Assert
            paginationSet.CurrentPage.Should().Be(1);
            paginationSet.TotalPages.Should().Be(1);
            paginationSet.TotalCount.Should().Be(0);
            paginationSet.TotalCountUnfiltered.Should().Be(0);
            paginationSet.Items.Should().BeEmpty();
        }
    }
}
