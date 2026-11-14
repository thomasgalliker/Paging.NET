using System.Text.Json;
using System.Text.Json.Serialization;

namespace Paging
{
    /// <summary>
    /// Serializes a <see cref="FilterNode"/> tree to and from a JSON string holding the
    /// canonical filter expression, e.g. <c>"(Brand contains \"bmw\" &amp;&amp; Year &gt;= 2020) || IsElectric == true"</c>.
    /// An empty or whitespace string reads as <c>null</c> (no filter).
    /// </summary>
    internal sealed class FilterNodeJsonConverter : JsonConverter<FilterNode>
    {
        public override FilterNode? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException($"Expected a filter expression string but found '{reader.TokenType}'.");
            }

            var expression = reader.GetString();

            try
            {
                return FilterExpressionParser.Parse(expression);
            }
            catch (FormatException ex)
            {
                throw new JsonException(ex.Message, ex);
            }
        }

        public override void Write(Utf8JsonWriter writer, FilterNode value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(FilterExpressionWriter.Write(value));
        }
    }
}
