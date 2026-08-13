using System.ComponentModel;

namespace Paging.Tests.Filtering
{
    /// <summary>
    /// The <see cref="TypeConverter"/> registered on <see cref="FilterNode"/> is what enables
    /// query-string model binding of <c>?filter=...</c> in ASP.NET Core (<c>[FromQuery] PagingInfo</c>).
    /// </summary>
    public class FilterNodeTypeConverterTests
    {
        private static TypeConverter GetConverter()
        {
            return TypeDescriptor.GetConverter(typeof(FilterNode));
        }

        [Fact]
        public void ShouldRegisterConverterOnFilterNode()
        {
            // Act
            var converter = GetConverter();

            // Assert
            converter.Should().BeOfType<FilterNodeTypeConverter>();
            converter.CanConvertFrom(typeof(string)).Should().BeTrue();
            converter.CanConvertTo(typeof(string)).Should().BeTrue();
        }

        [Fact]
        public void ShouldConvertFromString()
        {
            // Arrange
            var converter = GetConverter();

            // Act
            var node = converter.ConvertFrom("Year >= 2020 && Name contains \"bmw\"");

            // Assert
            var group = node.Should().BeOfType<FilterGroup>().Subject;
            group.Logic.Should().Be(FilterLogic.And);
            group.Nodes.Should().HaveCount(2);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void ShouldConvertFromEmptyString_AsNull(string expression)
        {
            // Arrange
            var converter = GetConverter();

            // Act
            var node = converter.ConvertFrom(expression);

            // Assert
            node.Should().BeNull();
        }

        [Fact]
        public void ShouldThrowFormatException_OnInvalidExpression()
        {
            // Arrange
            var converter = GetConverter();

            // Act
            Action action = () => converter.ConvertFrom("Year >>= 2020");

            // Assert: model binding surfaces this as a validation error (HTTP 400)
            action.Should().Throw<FormatException>()
                .WithMessage("*Invalid filter expression at position*");
        }

        [Fact]
        public void ShouldConvertToString_CanonicalForm()
        {
            // Arrange
            var converter = GetConverter();
            var node = FilterGroup.And(
                new FilterCondition("Year", FilterOperator.GreaterThanOrEqual, 2020),
                new FilterCondition("Name", FilterOperator.Contains, "bmw"));

            // Act
            var expression = converter.ConvertTo(node, typeof(string));

            // Assert
            expression.Should().Be("Year >= 2020 && Name contains \"bmw\"");
        }

        [Fact]
        public void ShouldRoundTripThroughConverter()
        {
            // Arrange: canonical form ('&&' binds tighter than '||', so no parentheses needed)
            var converter = GetConverter();
            const string expression = "Brand contains \"bmw\" && Year >= 2020 || IsElectric == true";

            // Act
            var node = (FilterNode)converter.ConvertFrom(expression)!;
            var roundTripped = converter.ConvertTo(node, typeof(string));

            // Assert
            roundTripped.Should().Be(expression);
        }
    }
}
