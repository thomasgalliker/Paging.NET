using System.Text.Json.Serialization;

namespace Paging
{
    [JsonConverter(typeof(SortOrderJsonConverter))]
    public enum SortOrder
    {
        Asc,
        Desc,
    }
}
