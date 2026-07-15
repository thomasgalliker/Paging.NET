namespace Paging.Tests.Filtering
{
    public class FilterExpressionWriterTests
    {
        [Fact]
        public void ShouldWriteSingleCondition()
        {
            // Arrange
            var node = new FilterCondition("Name", FilterOperator.Contains, "bmw");

            // Act
            var expression = node.ToString();

            // Assert
            expression.Should().Be("Name contains \"bmw\"");
        }

        [Theory]
        [InlineData(FilterOperator.Equal, "Year == 2020")]
        [InlineData(FilterOperator.NotEqual, "Year != 2020")]
        [InlineData(FilterOperator.GreaterThan, "Year > 2020")]
        [InlineData(FilterOperator.GreaterThanOrEqual, "Year >= 2020")]
        [InlineData(FilterOperator.LessThan, "Year < 2020")]
        [InlineData(FilterOperator.LessThanOrEqual, "Year <= 2020")]
        public void ShouldWriteComparisonOperators(FilterOperator filterOperator, string expected)
        {
            // Arrange
            var node = new FilterCondition("Year", filterOperator, 2020);

            // Act
            var expression = node.ToString();

            // Assert
            expression.Should().Be(expected);
        }

        [Fact]
        public void ShouldWriteBooleanAndNullValues()
        {
            // Arrange
            var boolNode = new FilterCondition("IsElectric", FilterOperator.Equal, true);
            var nullNode = new FilterCondition("Name", FilterOperator.Equal, null);

            // Act
            var boolExpression = boolNode.ToString();
            var nullExpression = nullNode.ToString();

            // Assert
            boolExpression.Should().Be("IsElectric == true");
            nullExpression.Should().Be("Name == null");
        }

        [Fact]
        public void ShouldWriteInListValue()
        {
            // Arrange
            var node = new FilterCondition("Id", FilterOperator.In, new object[] { 1, 2, 3 });

            // Act
            var expression = node.ToString();

            // Assert
            expression.Should().Be("Id in [1, 2, 3]");
        }

        [Theory]
        [InlineData(FilterOperator.NotContains, "Name !contains \"bmw\"")]
        [InlineData(FilterOperator.NotStartsWith, "Name !startswith \"bmw\"")]
        [InlineData(FilterOperator.NotEndsWith, "Name !endswith \"bmw\"")]
        public void ShouldWriteNegatedStringOperators(FilterOperator filterOperator, string expected)
        {
            // Arrange
            var node = new FilterCondition("Name", filterOperator, "bmw");

            // Act
            var expression = node.ToString();

            // Assert
            expression.Should().Be(expected);
        }

        [Fact]
        public void ShouldWriteNotInListValue()
        {
            // Arrange
            var node = new FilterCondition("Id", FilterOperator.NotIn, new object[] { 1, 2, 3 });

            // Act
            var expression = node.ToString();

            // Assert
            expression.Should().Be("Id !in [1, 2, 3]");
        }

        [Fact]
        public void ShouldEscapeQuotesInStringValue()
        {
            // Arrange
            var node = new FilterCondition("Name", FilterOperator.Equal, "a \"quoted\" value");

            // Act
            var expression = node.ToString();

            // Assert
            expression.Should().Be("Name == \"a \\\"quoted\\\" value\"");
        }

        [Fact]
        public void ShouldOmitParenthesesWhenAndBindsTighterThanOr()
        {
            // Arrange: Or(And(A, B), C) needs no parentheses since && binds tighter than ||
            var node = FilterGroup.Or(
                FilterGroup.And(
                    new FilterCondition("A", FilterOperator.Equal, 1),
                    new FilterCondition("B", FilterOperator.Equal, 2)),
                new FilterCondition("C", FilterOperator.Equal, 3));

            // Act
            var expression = node.ToString();

            // Assert
            expression.Should().Be("A == 1 && B == 2 || C == 3");
        }

        [Fact]
        public void ShouldAddParenthesesAroundOrGroupNestedInAndGroup()
        {
            // Arrange: And(A, Or(B, C)) requires parentheses around the Or group
            var node = FilterGroup.And(
                new FilterCondition("A", FilterOperator.Equal, 1),
                FilterGroup.Or(
                    new FilterCondition("B", FilterOperator.Equal, 2),
                    new FilterCondition("C", FilterOperator.Equal, 3)));

            // Act
            var expression = node.ToString();

            // Assert
            expression.Should().Be("A == 1 && (B == 2 || C == 3)");
        }

        [Theory]
        [InlineData("Brand contains \"bmw\" && Year >= 2020 || IsElectric == true")]
        [InlineData("A == 1 && (B == 2 || C == 3)")]
        [InlineData("Id in [1, 2, 3]")]
        [InlineData("Name == null")]
        [InlineData("Price >= 5000.5")]
        [InlineData("Name !contains \"bmw\"")]
        [InlineData("Kind !in [\"a\", \"b\"]")]
        public void ShouldRoundTripCanonicalForm(string expression)
        {
            // Arrange: the canonical form of a parsed expression re-parses and re-writes identically
            var canonical = FilterNode.Parse(expression)!.ToString();

            // Act
            var roundTripped = FilterNode.Parse(canonical)!.ToString();

            // Assert
            roundTripped.Should().Be(canonical);
        }
    }
}
