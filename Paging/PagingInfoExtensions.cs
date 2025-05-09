namespace Paging
{
    public static class PagingInfoExtensions
    {
        /// <summary>
        /// Maps Items of <paramref name="paginationSet"/> into a new <see cref="PaginationSet{TTarget}"/>
        /// using the mapping logic in parameter <paramref name="mapSourceToTarget"/>.
        /// </summary>
        /// <typeparam name="TSource">Source type (e.g. entity type).</typeparam>
        /// <typeparam name="TTarget">Target type (e.g. DTO, ViewModel type).</typeparam>
        /// <param name="pagingInfo">The source paginationInfo.</param>
        /// <param name="paginationSet">The source paginationSet.</param>
        /// <param name="mapSourceToTarget">The mapping logic which maps <see cref="IEnumerable{TSource}"/> to <see cref="IEnumerable{TTarget}"/>.</param>
        public static PaginationSet<TTarget> Map<TSource, TTarget>(this PagingInfo pagingInfo, PaginationSet<TSource> paginationSet, Func<IEnumerable<TSource>, IEnumerable<TTarget>> mapSourceToTarget)
        {
            var sourceItems = paginationSet.Items;
            var targetItems = mapSourceToTarget(sourceItems);
            var paginationSetTarget = new PaginationSet<TTarget>(pagingInfo, targetItems, paginationSet.TotalCount, paginationSet.TotalCountUnfiltered);
            return paginationSetTarget;
        }

        public static IReadOnlyDictionary<string, SortOrder> ToSorting(this string sortBy)
        {
            if (string.IsNullOrEmpty(sortBy))
            {
                return null;
            }

            var sorting = sortBy.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s =>
                {
                    var sortSplit = s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    string key = null;
                    if (sortSplit.Length >= 1)
                    {
                        key = sortSplit[0];
                    }

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

        public static string ToSortByString(this IReadOnlyDictionary<string, SortOrder> sorting)
        {
            string sortBy;
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

        internal static string ToQueryString(this PagingInfo pagingInfo)
        {
            // Get all properties on the object
            var properties = new Dictionary<string, string>
            {
                { nameof(PagingInfo.CurrentPage), $"{pagingInfo.CurrentPage}" },
                { nameof(PagingInfo.ItemsPerPage), $"{pagingInfo.ItemsPerPage}" }
            };

            if (!string.IsNullOrEmpty(pagingInfo.SortBy))
            {
                properties.Add(nameof(PagingInfo.SortBy), pagingInfo.SortBy);
            }

            if (pagingInfo.Reverse)
            {
                properties.Add(nameof(PagingInfo.Reverse), $"{pagingInfo.Reverse}");
            }

            if (!string.IsNullOrEmpty(pagingInfo.Search))
            {
                properties.Add(nameof(PagingInfo.Search), pagingInfo.Search);
            }

            // Concat all key/value pairs into a string separated by ampersand
            return string.Join("&", properties.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
        }
    }
}
