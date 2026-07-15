namespace Paging.Tests.Filtering
{
    public class FilterNodeExtensionsTests
    {
        [Fact]
        public void ShouldReturnRight_WhenLeftIsNull()
        {
            // Arrange
            FilterNode? left = null;
            var right = new FilterCondition("A", FilterOperator.Equal, 1);

            // Act
            var result = left.And(right);

            // Assert
            result.Should().BeSameAs(right);
        }

        [Fact]
        public void ShouldReturnLeft_WhenRightIsNull()
        {
            // Arrange
            var left = new FilterCondition("A", FilterOperator.Equal, 1);

            // Act
            var result = left.And(null);

            // Assert
            result.Should().BeSameAs(left);
        }

        [Fact]
        public void ShouldReturnNull_WhenBothSidesAreNull()
        {
            // Arrange
            FilterNode? left = null;

            // Act
            var result = left.And(null);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ShouldCombineTwoConditionsIntoFlatAndGroup()
        {
            // Arrange
            var a = new FilterCondition("A", FilterOperator.Equal, 1);
            var b = new FilterCondition("B", FilterOperator.Equal, 2);

            // Act
            var result = a.And(b);

            // Assert
            var group = result.Should().BeOfType<FilterGroup>().Subject;
            group.Logic.Should().Be(FilterLogic.And);
            group.Nodes.Should().HaveCount(2);
        }

        [Fact]
        public void ShouldFlattenChainedAndIntoSingleGroup()
        {
            // Arrange
            var a = new FilterCondition("A", FilterOperator.Equal, 1);
            var b = new FilterCondition("B", FilterOperator.Equal, 2);
            var c = new FilterCondition("C", FilterOperator.Equal, 3);

            // Act
            var result = a.And(b).And(c);

            // Assert
            var group = result.Should().BeOfType<FilterGroup>().Subject;
            group.Logic.Should().Be(FilterLogic.And);
            group.Nodes.Should().HaveCount(3);
        }

        [Fact]
        public void ShouldFlattenChainedOrIntoSingleGroup()
        {
            // Arrange
            var a = new FilterCondition("A", FilterOperator.Equal, 1);
            var b = new FilterCondition("B", FilterOperator.Equal, 2);
            var c = new FilterCondition("C", FilterOperator.Equal, 3);

            // Act
            var result = a.Or(b).Or(c);

            // Assert
            var group = result.Should().BeOfType<FilterGroup>().Subject;
            group.Logic.Should().Be(FilterLogic.Or);
            group.Nodes.Should().HaveCount(3);
        }

        [Fact]
        public void ShouldAndFilterIntoPagingInfo_AndReturnSameInstance()
        {
            // Arrange
            var pagingInfo = new PagingInfo { Filter = new FilterCondition("A", FilterOperator.Equal, 1) };
            var extra = new FilterCondition("B", FilterOperator.Equal, 2);

            // Act
            var result = pagingInfo.AndFilter(extra);

            // Assert
            result.Should().BeSameAs(pagingInfo);
            var group = pagingInfo.Filter.Should().BeOfType<FilterGroup>().Subject;
            group.Logic.Should().Be(FilterLogic.And);
            group.Nodes.Should().HaveCount(2);
        }

        [Fact]
        public void ShouldAndFilterIntoPagingInfo_WhenExistingFilterIsNull()
        {
            // Arrange
            var pagingInfo = new PagingInfo();
            var filter = new FilterCondition("A", FilterOperator.Equal, 1);

            // Act
            pagingInfo.AndFilter(filter);

            // Assert
            pagingInfo.Filter.Should().BeSameAs(filter);
        }
    }
}
