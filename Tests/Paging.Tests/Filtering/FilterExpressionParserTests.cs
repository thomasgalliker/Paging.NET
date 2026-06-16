namespace Paging.Tests.Filtering
{
    public class FilterExpressionParserTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ShouldParseNullOrWhitespaceAsNull(string? expression)
        {
            // Act
            var node = FilterNode.Parse(expression);

            // Assert
            node.Should().BeNull();
        }

        [Fact]
        public void ShouldParseSingleCondition()
        {
            // Arrange
            const string expression = "Name contains \"bmw\"";

            // Act
            var node = FilterNode.Parse(expression);

            // Assert
            var condition = node.Should().BeOfType<FilterCondition>().Subject;
            condition.Property.Should().Be("Name");
            condition.Operator.Should().Be(FilterOperator.Contains);
            condition.Value.Should().Be("bmw");
        }

        [Theory]
        [InlineData("Year == 2020", FilterOperator.Equal)]
        [InlineData("Year != 2020", FilterOperator.NotEqual)]
        [InlineData("Year > 2020", FilterOperator.GreaterThan)]
        [InlineData("Year >= 2020", FilterOperator.GreaterThanOrEqual)]
        [InlineData("Year < 2020", FilterOperator.LessThan)]
        [InlineData("Year <= 2020", FilterOperator.LessThanOrEqual)]
        [InlineData("Name startswith \"b\"", FilterOperator.StartsWith)]
        [InlineData("Name endswith \"w\"", FilterOperator.EndsWith)]
        public void ShouldParseOperators(string expression, FilterOperator expectedOperator)
        {
            // Act
            var node = FilterNode.Parse(expression);

            // Assert
            node.Should().BeOfType<FilterCondition>().Which.Operator.Should().Be(expectedOperator);
        }

        [Fact]
        public void ShouldParseStringValue()
        {
            // Act
            var node = FilterNode.Parse("A == \"text\"");

            // Assert
            node.Should().BeOfType<FilterCondition>().Which.Value.Should().Be("text");
        }

        [Fact]
        public void ShouldParseIntegerValueAsLong()
        {
            // Act
            var node = FilterNode.Parse("A == 42");

            // Assert
            node.Should().BeOfType<FilterCondition>().Which.Value.Should().Be(42L);
        }

        [Fact]
        public void ShouldParseDecimalValueAsDouble()
        {
            // Act
            var node = FilterNode.Parse("A == 4.5");

            // Assert
            node.Should().BeOfType<FilterCondition>().Which.Value.Should().Be(4.5d);
        }

        [Theory]
        [InlineData("A == true", true)]
        [InlineData("A == false", false)]
        public void ShouldParseBooleanValue(string expression, bool expectedValue)
        {
            // Act
            var node = FilterNode.Parse(expression);

            // Assert
            node.Should().BeOfType<FilterCondition>().Which.Value.Should().Be(expectedValue);
        }

        [Fact]
        public void ShouldParseNullValue()
        {
            // Act
            var node = FilterNode.Parse("A == null");

            // Assert
            node.Should().BeOfType<FilterCondition>().Which.Value.Should().BeNull();
        }

        [Fact]
        public void ShouldParseInListValue()
        {
            // Act
            var node = FilterNode.Parse("Id in [1, 2, 3]");

            // Assert
            var condition = node.Should().BeOfType<FilterCondition>().Subject;
            condition.Operator.Should().Be(FilterOperator.In);
            condition.Value.Should().BeEquivalentTo(new object[] { 1L, 2L, 3L });
        }

        [Fact]
        public void ShouldApplyPrecedence_AndBindsTighterThanOr()
        {
            // Arrange: A && B || C  =>  Or(And(A, B), C)
            const string expression = "A == 1 && B == 2 || C == 3";

            // Act
            var node = FilterNode.Parse(expression);

            // Assert
            var root = node.Should().BeOfType<FilterGroup>().Subject;
            root.Logic.Should().Be(FilterLogic.Or);
            root.Nodes.Should().HaveCount(2);
            root.Nodes[0].Should().BeOfType<FilterGroup>().Which.Logic.Should().Be(FilterLogic.And);
            root.Nodes[1].Should().BeOfType<FilterCondition>();
        }

        [Fact]
        public void ShouldRespectParentheses()
        {
            // Arrange: A && (B || C)  =>  And(A, Or(B, C))
            const string expression = "A == 1 && (B == 2 || C == 3)";

            // Act
            var node = FilterNode.Parse(expression);

            // Assert
            var root = node.Should().BeOfType<FilterGroup>().Subject;
            root.Logic.Should().Be(FilterLogic.And);
            root.Nodes[1].Should().BeOfType<FilterGroup>().Which.Logic.Should().Be(FilterLogic.Or);
        }

        [Fact]
        public void ShouldFlattenRepeatedOperatorIntoSingleGroup()
        {
            // Act
            var node = FilterNode.Parse("A == 1 && B == 2 && C == 3");

            // Assert
            var root = node.Should().BeOfType<FilterGroup>().Subject;
            root.Logic.Should().Be(FilterLogic.And);
            root.Nodes.Should().HaveCount(3);
        }

        [Fact]
        public void ShouldParseEscapedQuotesInStringValue()
        {
            // Arrange
            const string expression = "Name == \"a \\\"quoted\\\" value\"";

            // Act
            var node = FilterNode.Parse(expression);

            // Assert
            node.Should().BeOfType<FilterCondition>().Which.Value.Should().Be("a \"quoted\" value");
        }

        [Theory]
        [InlineData("Year >=")]              // missing value
        [InlineData("Year 2020")]            // missing operator
        [InlineData("== 2020")]              // missing property
        [InlineData("(Year == 2020")]        // unbalanced parenthesis
        [InlineData("Year === 2020")]        // invalid token
        [InlineData("Name contains bmw")]    // unquoted string value
        [InlineData("Id in 1")]              // 'in' requires a list
        public void ShouldThrowFormatException_OnInvalidExpression(string expression)
        {
            // Act
            Action act = () => FilterNode.Parse(expression);

            // Assert
            act.Should().Throw<FormatException>();
        }

        [Fact]
        public void ShouldTryParse_ReturnTrueAndNode_OnValidExpression()
        {
            // Act
            var success = FilterNode.TryParse("Year >= 2020", out var node);

            // Assert
            success.Should().BeTrue();
            node.Should().BeOfType<FilterCondition>();
        }

        [Fact]
        public void ShouldTryParse_ReturnFalseAndNull_OnInvalidExpression()
        {
            // Act
            var success = FilterNode.TryParse("Year >=", out var node);

            // Assert
            success.Should().BeFalse();
            node.Should().BeNull();
        }

        [Fact]
        public void ShouldImplicitlyConvertStringToFilterNode()
        {
            // Act
            FilterNode node = "Year >= 2020";

            // Assert
            var condition = node.Should().BeOfType<FilterCondition>().Subject;
            condition.Property.Should().Be("Year");
            condition.Operator.Should().Be(FilterOperator.GreaterThanOrEqual);
            condition.Value.Should().Be(2020L);
        }

        [Fact]
        public void ShouldImplicitlyConvertNullStringToNull()
        {
            // Arrange
            string? expression = null;

            // Act
            FilterNode? node = expression;

            // Assert
            node.Should().BeNull();
        }
    }
}
