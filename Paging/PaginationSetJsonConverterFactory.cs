using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Paging
{
    internal class PaginationSetJsonConverterFactory : JsonConverterFactory
    {
        private static readonly ConcurrentDictionary<Type, JsonConverter> ConverterCache = new();

        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(PaginationSet<>);
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            return ConverterCache.GetOrAdd(typeToConvert, static paginationSetType =>
            {
                var itemType = paginationSetType.GetGenericArguments()[0];
                var converterType = typeof(PaginationSetJsonConverter<>).MakeGenericType(itemType);
                return (JsonConverter)Activator.CreateInstance(converterType)!;
            });
        }

        private sealed class PaginationSetJsonConverter<T> : JsonConverter<PaginationSet<T>>
        {
            public override PaginationSet<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new JsonException($"Expected {JsonTokenType.StartObject} token.");
                }

                var paginationSet = new PaginationSet<T>();

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        return paginationSet;
                    }

                    if (reader.TokenType != JsonTokenType.PropertyName)
                    {
                        throw new JsonException($"Expected {JsonTokenType.PropertyName} token.");
                    }

                    var propertyName = reader.GetString();
                    reader.Read();

                    switch (propertyName?.ToLowerInvariant())
                    {
                        case "firstpageindex":
                            paginationSet.FirstPageIndex = ReadInt32(ref reader);
                            break;
                        case "currentpage":
                            paginationSet.CurrentPage = ReadInt32(ref reader);
                            break;
                        case "totalpages":
                            paginationSet.TotalPages = ReadInt32(ref reader);
                            break;
                        case "totalcount":
                            paginationSet.TotalCount = ReadInt32(ref reader);
                            break;
                        case "totalcountunfiltered":
                            paginationSet.TotalCountUnfiltered = ReadInt32(ref reader);
                            break;
                        case "items":
                            paginationSet.Items = JsonSerializer.Deserialize<IEnumerable<T>>(ref reader, options) ?? Enumerable.Empty<T>();
                            break;
                        default:
                            reader.Skip();
                            break;
                    }
                }

                throw new JsonException("Unexpected end of JSON while reading PaginationSet.");
            }

            public override void Write(Utf8JsonWriter writer, PaginationSet<T> value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                if (value.FirstPageIndex != PagingInfo.DefaultFirstPageIndex)
                {
                    writer.WriteNumber("firstPageIndex", value.FirstPageIndex);
                }

                writer.WriteNumber("currentPage", value.CurrentPage);
                writer.WriteNumber("totalPages", value.TotalPages);
                writer.WriteNumber("totalCount", value.TotalCount);
                writer.WriteNumber("totalCountUnfiltered", value.TotalCountUnfiltered);
                writer.WritePropertyName("items");
                JsonSerializer.Serialize(writer, value.Items, options);
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
}
