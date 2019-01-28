using System;
using System.Collections.Generic;
using System.Linq;

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
