namespace Paging.Tests.Filtering
{
    public class FilterNodeTests
    {
        [Fact]
        public void ShouldBeEqual_ForConditionsWithSamePropertyOperatorAndValue()
        {
            // Arrange
            var condition1 = new FilterCondition("Year", FilterOperator.Equal, 2020);
            var condition2 = new FilterCondition("Year", FilterOperator.Equal, 2020);

            // Act
            var areEqual = condition1.Equals(condition2);

            // Assert
            areEqual.Should().BeTrue();
            condition1.GetHashCode().Should().Be(condition2.GetHashCode());
        }

        [Theory]
        [InlineData("Year", FilterOperator.Equal, 2021)]
        [InlineData("Year", FilterOperator.GreaterThan, 2020)]
        [InlineData("Make", FilterOperator.Equal, 2020)]
        public void ShouldNotBeEqual_ForConditionsThatDiffer(string property, FilterOperator filterOperator, int value)
        {
            // Arrange
            var condition = new FilterCondition("Year", FilterOperator.Equal, 2020);
            var other = new FilterCondition(property, filterOperator, value);

            // Act
            var areEqual = condition.Equals(other);

            // Assert
            areEqual.Should().BeFalse();
        }

        [Fact]
        public void ShouldBeEqual_ForGroupsWithSameLogicAndNodesInSameOrder()
        {
            // Arrange
            var group1 = FilterGroup.And(
                new FilterCondition("A", FilterOperator.Equal, 1),
                new FilterCondition("B", FilterOperator.Equal, 2));
            var group2 = FilterGroup.And(
                new FilterCondition("A", FilterOperator.Equal, 1),
                new FilterCondition("B", FilterOperator.Equal, 2));

            // Act
            var areEqual = group1.Equals(group2);

            // Assert
            areEqual.Should().BeTrue();
            group1.GetHashCode().Should().Be(group2.GetHashCode());
        }

        [Fact]
        public void ShouldNotBeEqual_ForGroupsThatDifferInLogic()
        {
            // Arrange
            var andGroup = FilterGroup.And(new FilterCondition("A", FilterOperator.Equal, 1));
            var orGroup = FilterGroup.Or(new FilterCondition("A", FilterOperator.Equal, 1));

            // Act
            var areEqual = andGroup.Equals(orGroup);

            // Assert
            areEqual.Should().BeFalse();
        }

        [Fact]
        public void ShouldNotBeEqual_ForGroupsThatDifferInNodeOrder()
        {
            // Arrange
            var a = new FilterCondition("A", FilterOperator.Equal, 1);
            var b = new FilterCondition("B", FilterOperator.Equal, 2);
            var group1 = FilterGroup.And(a, b);
            var group2 = FilterGroup.And(b, a);

            // Act
            var areEqual = group1.Equals(group2);

            // Assert
            areEqual.Should().BeFalse();
        }

        [Fact]
        public void ShouldCreateAndGroup_WithAndLogic()
        {
            // Act
            var group = FilterGroup.And(
                new FilterCondition("A", FilterOperator.Equal, 1),
                new FilterCondition("B", FilterOperator.Equal, 2));

            // Assert
            group.Logic.Should().Be(FilterLogic.And);
            group.Nodes.Should().HaveCount(2);
        }

        [Fact]
        public void ShouldCreateOrGroup_WithOrLogic()
        {
            // Act
            var group = FilterGroup.Or(
                new FilterCondition("A", FilterOperator.Equal, 1),
                new FilterCondition("B", FilterOperator.Equal, 2));

            // Assert
            group.Logic.Should().Be(FilterLogic.Or);
            group.Nodes.Should().HaveCount(2);
        }

        [Fact]
        public void ShouldNotBeEqual_ForConditionAndGroup()
        {
            // Arrange
            var condition = new FilterCondition("A", FilterOperator.Equal, 1);
            var group = FilterGroup.And(new FilterCondition("A", FilterOperator.Equal, 1));

            // Act
            var areEqual = condition.Equals((FilterNode)group);

            // Assert
            areEqual.Should().BeFalse();
        }
    }
}
