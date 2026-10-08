namespace Paging.Tests
{
    public class SortOrderJsonConverterTests
    {
        [Theory]
        [InlineData("1", SortOrder.Asc)]
        [InlineData("-1", SortOrder.Desc)]
        [InlineData("0", SortOrder.None)]
        public void ShouldDeserializeFromNumber(string json, SortOrder expected)
        {
            // Act
            var sortOrder = JsonSerializer.Deserialize<SortOrder>(json);

            // Assert
            sortOrder.Should().Be(expected);
        }

        [Theory]
        [InlineData("\"asc\"", SortOrder.Asc)]
        [InlineData("\"Asc\"", SortOrder.Asc)]
        [InlineData("\"desc\"", SortOrder.Desc)]
        [InlineData("\"DESC\"", SortOrder.Desc)]
        [InlineData("\"none\"", SortOrder.None)]
        [InlineData("\"1\"", SortOrder.Asc)]
        [InlineData("\"-1\"", SortOrder.Desc)]
        [InlineData("\"0\"", SortOrder.None)]
        public void ShouldDeserializeFromString(string json, SortOrder expected)
        {
            // Act
            var sortOrder = JsonSerializer.Deserialize<SortOrder>(json);

            // Assert
            sortOrder.Should().Be(expected);
        }

        [Theory]
        [InlineData(SortOrder.Asc, "1")]
        [InlineData(SortOrder.Desc, "-1")]
        [InlineData(SortOrder.None, "0")]
        public void ShouldSerializeToNumber(SortOrder sortOrder, string expectedJson)
        {
            // Act
            var json = JsonSerializer.Serialize(sortOrder);

            // Assert
            json.Should().Be(expectedJson);
        }

        [Theory]
        [InlineData("2")]
        [InlineData("-5")]
        [InlineData("\"5\"")]
        [InlineData("\"ascending\"")]
        [InlineData("null")]
        public void ShouldThrowForUndefinedValue(string json)
        {
            // Act
            Action action = () => JsonSerializer.Deserialize<SortOrder>(json);

            // Assert
            action.Should().Throw<JsonException>();
        }
    }
}
