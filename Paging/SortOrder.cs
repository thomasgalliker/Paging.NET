using System.Text.Json.Serialization;

namespace Paging
{
    /// <summary>
    /// Sort direction for a property. The underlying integer values follow the Angular/PrimeNG
    /// convention (<c>1 = ascending</c>, <c>-1 = descending</c>, <c>0 = none</c>).
    /// </summary>
    [JsonConverter(typeof(SortOrderJsonConverter))]
    public enum SortOrder
    {
        /// <summary>
        /// No sort order. The property is ignored when sorting is applied.
        /// </summary>
        None = 0,

        /// <summary>
        /// Ascending order.
        /// </summary>
        Asc = 1,

        /// <summary>
        /// Descending order.
        /// </summary>
        Desc = -1,
    }
}
