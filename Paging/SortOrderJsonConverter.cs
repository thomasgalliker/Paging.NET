using System.Text.Json;
using System.Text.Json.Serialization;

namespace Paging
{
    internal class SortOrderJsonConverter : JsonConverter<SortOrder>
    {
        public override SortOrder Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var enumString = reader.GetString();
                if (Enum.TryParse<SortOrder>(enumString, true, out var sortOrder))
                {
                    return sortOrder;
                }
            }

            if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var enumValue))
            {
                return (SortOrder)enumValue;
            }

            throw new JsonException($"Unable to convert value to {nameof(SortOrder)}.");
        }

        public override void Write(Utf8JsonWriter writer, SortOrder value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue((int)value);
        }
    }
}
