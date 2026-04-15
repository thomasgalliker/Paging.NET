using System.Text.Json;
using System.Text.Json.Serialization;

namespace Paging
{
    internal class PagingInfoJsonConverter : JsonConverter<PagingInfo>
    {
        public override PagingInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Expected {JsonTokenType.StartObject} token.");
            }

            var pagingInfo = new PagingInfo();
            string? sortBy = null;
            Dictionary<string, SortOrder>? sorting = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    if (!string.IsNullOrWhiteSpace(sortBy))
                    {
                        pagingInfo.SortBy = sortBy;
                    }
                    else if (sorting is not null)
                    {
                        pagingInfo.Sorting = sorting;
                    }

                    return pagingInfo;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException($"Expected {JsonTokenType.PropertyName} token.");
                }

                var propertyName = reader.GetString();
                reader.Read();

                switch (propertyName?.ToLowerInvariant())
                {
                    case "currentpage":
                        pagingInfo.CurrentPage = ReadInt32(ref reader);
                        break;
                    case "itemsperpage":
                        pagingInfo.ItemsPerPage = ReadInt32(ref reader);
                        break;
                    case "sortby":
                        sortBy = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
                        break;
                    case "sorting":
                        sorting = JsonSerializer.Deserialize<Dictionary<string, SortOrder>>(ref reader, options);
                        break;
                    case "reverse":
                        pagingInfo.Reverse = reader.TokenType == JsonTokenType.String
                            ? bool.Parse(reader.GetString()!)
                            : reader.GetBoolean();
                        break;
                    case "search":
                        pagingInfo.Search = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
                        break;
                    case "filter":
                        pagingInfo.Filter = JsonSerializer.Deserialize<Dictionary<string, object?>>(ref reader, options)
                            ?? new Dictionary<string, object?>();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            throw new JsonException("Unexpected end of JSON while reading PagingInfo.");
        }

        public override void Write(Utf8JsonWriter writer, PagingInfo value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("currentPage", value.CurrentPage);
            writer.WriteNumber("itemsPerPage", value.ItemsPerPage);

            if (value.SortBy is null)
            {
                writer.WriteNull("sortBy");
            }
            else
            {
                writer.WriteString("sortBy", value.SortBy);
            }

            writer.WritePropertyName("sorting");
            JsonSerializer.Serialize(writer, value.Sorting, options);

            writer.WriteBoolean("reverse", value.Reverse);

            if (value.Search is null)
            {
                writer.WriteNull("search");
            }
            else
            {
                writer.WriteString("search", value.Search);
            }

            writer.WritePropertyName("filter");
            JsonSerializer.Serialize(writer, value.Filter, options);
            writer.WriteEndObject();
        }

        private static int ReadInt32(ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var numberValue))
            {
                return numberValue;
            }

            if (reader.TokenType == JsonTokenType.String && int.TryParse(reader.GetString(), out var stringValue))
            {
                return stringValue;
            }

            throw new JsonException("Unable to convert JSON value to Int32.");
        }
    }
}
