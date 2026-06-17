using System.Collections.ObjectModel;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Paging
{
    public static class PagingInfoExtensions
    {
        /// <summary>
        /// Converts a <c>SortBy</c> string into a sorting dictionary keyed by property name.
        /// </summary>
        /// <param name="sortBy">The sort expression, for example <c>Name Asc, Created Desc</c>.</param>
        /// <returns>A read-only dictionary containing the parsed sort order per property.</returns>
        public static IReadOnlyDictionary<string, SortOrder> ToSorting(this string? sortBy)
        {
            if (string.IsNullOrEmpty(sortBy))
            {
                return new ReadOnlyDictionary<string, SortOrder>(new Dictionary<string, SortOrder>());
            }

            var sorting = sortBy!.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s =>
                {
                    var sortSplit = s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    var key = sortSplit.Length >= 1 ? sortSplit[0] : string.Empty;

                    var value = SortOrder.Asc;
                    if (sortSplit.Length == 2)
                    {
                        value = (SortOrder)Enum.Parse(typeof(SortOrder), sortSplit[1], ignoreCase: true);
                    }

                    return new { Key = key, Value = value };
                })
                .ToDictionary(s => s.Key, pair => pair.Value);

            return sorting;
        }

        /// <summary>
        /// Converts a sorting dictionary into a <c>SortBy</c> string representation.
        /// </summary>
        /// <param name="sorting">The sorting dictionary to convert.</param>
        /// <returns>
        /// A comma-separated sort expression, or <c>null</c> when <paramref name="sorting"/> is
        /// <c>null</c> or empty.
        /// </returns>
        public static string? ToSortByString(this IReadOnlyDictionary<string, SortOrder>? sorting)
        {
            string? sortBy;

            if (sorting == null)
            {
                sortBy = null;
            }
            else
            {
                sortBy = string.Join(", ", sorting.Select(kvp => $"{kvp.Key} {kvp.Value}"));
                if (sortBy == string.Empty)
                {
                    sortBy = null;
                }
            }

            return sortBy;
        }

        /// <summary>
        /// Converts a <see cref="PagingInfo"/> instance into query string parameters.
        /// </summary>
        /// <param name="pagingInfo">The paging information to serialize.</param>
        /// <returns>A read-only dictionary of query parameter names and values.</returns>
        public static IReadOnlyDictionary<string, string> ToQueryParameters(this PagingInfo pagingInfo)
        {
            // Get all properties on the object
            var properties = new Dictionary<string, string>
            {
                { ToJsonName(nameof(PagingInfo.CurrentPage)), $"{pagingInfo.CurrentPage}" }
            };

            if (pagingInfo.FirstPageIndex != PagingInfo.DefaultFirstPageIndex)
            {
                properties.Add(ToJsonName(nameof(PagingInfo.FirstPageIndex)), $"{pagingInfo.FirstPageIndex}");
            }

            if (pagingInfo.ItemsPerPage is int itemsPerPage)
            {
                properties.Add(ToJsonName(nameof(PagingInfo.ItemsPerPage)), $"{itemsPerPage}");
            }

            if (!string.IsNullOrEmpty(pagingInfo.SortBy))
            {
                properties.Add(ToJsonName(nameof(PagingInfo.SortBy)), pagingInfo.SortBy!);
            }

            if (pagingInfo.Reverse)
            {
                properties.Add(ToJsonName(nameof(PagingInfo.Reverse)), $"{pagingInfo.Reverse}");
            }

            if (!string.IsNullOrEmpty(pagingInfo.Search))
            {
                properties.Add(ToJsonName(nameof(PagingInfo.Search)), pagingInfo.Search!);
            }

            return new ReadOnlyDictionary<string, string>(properties);
        }

        // Maps PagingInfo CLR property names to their JSON property names so the query string
        // shares the same camelCase contract as JSON serialization. The [JsonPropertyName]
        // attributes on PagingInfo remain the single source of truth for the wire format.
        private static readonly IReadOnlyDictionary<string, string> JsonPropertyNames = typeof(PagingInfo)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => new { p.Name, JsonName = p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name })
            .Where(p => p.JsonName != null)
            .ToDictionary(p => p.Name, p => p.JsonName!);

        private static string ToJsonName(string propertyName)
        {
            return JsonPropertyNames.TryGetValue(propertyName, out var value) ? value : throw new InvalidOperationException($"Property {propertyName} does not have a JsonPropertyNameAttribute");
        }

        /// <summary>
        /// Converts a <see cref="PagingInfo"/> instance into a URL-encoded query string.
        /// </summary>
        /// <param name="pagingInfo">The paging information to serialize.</param>
        /// <returns>A URL-encoded query string without a leading question mark.</returns>
        public static string ToQueryString(this PagingInfo pagingInfo)
        {
            return string.Join("&", pagingInfo.ToQueryParameters().Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
        }
    }
}
