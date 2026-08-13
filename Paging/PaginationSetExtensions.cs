namespace Paging
{
    public static class PaginationSetExtensions
    {
        /// <summary>
        /// Maps the Items of <paramref name="paginationSet"/> into a new <see cref="PaginationSet{TTarget}"/>
        /// using the mapping logic in parameter <paramref name="map"/>.
        /// All paging metadata (page indexes, totals) is carried over unchanged.
        /// </summary>
        /// <typeparam name="TSource">Source type (e.g. entity type).</typeparam>
        /// <typeparam name="TTarget">Target type (e.g. DTO, ViewModel type).</typeparam>
        /// <param name="paginationSet">The source paginationSet.</param>
        /// <param name="map">The mapping logic which maps <see cref="IEnumerable{TSource}"/> to <see cref="IEnumerable{TTarget}"/>.</param>
        /// <returns>A new <see cref="PaginationSet{TTarget}"/> containing the mapped items.</returns>
        public static PaginationSet<TTarget> Map<TSource, TTarget>(this PaginationSet<TSource> paginationSet, Func<IEnumerable<TSource>, IEnumerable<TTarget>> map)
        {
            if (paginationSet == null)
            {
                throw new ArgumentNullException(nameof(paginationSet));
            }

            if (map == null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            return new PaginationSet<TTarget>
            {
                FirstPageIndex = paginationSet.FirstPageIndex,
                CurrentPage = paginationSet.CurrentPage,
                TotalPages = paginationSet.TotalPages,
                TotalCount = paginationSet.TotalCount,
                TotalCountUnfiltered = paginationSet.TotalCountUnfiltered,
                Items = map(paginationSet.Items)
            };
        }

        /// <summary>
        /// Determines whether infinite scrolling can load more items, i.e. whether not all available
        /// items have been loaded yet.
        /// </summary>
        /// <typeparam name="T">The item type contained in the pagination set.</typeparam>
        /// <param name="paginationSet">The most recently loaded pagination set, or <c>null</c> before the first load.</param>
        /// <param name="pagingInfo">The paging configuration used to load items.</param>
        /// <returns>
        /// <see langword="true"/> when more items can be loaded; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool CanLoadMore<T>(this PaginationSet<T>? paginationSet, PagingInfo pagingInfo)
        {
            if (paginationSet == null)
            {
                return true;
            }

            if (pagingInfo == null)
            {
                throw new ArgumentNullException(nameof(pagingInfo));
            }

            if (pagingInfo.ItemsPerPage is null or 0)
            {
                return false;
            }

            var pagesLoaded = pagingInfo.CurrentPage - pagingInfo.FirstPageIndex + 1;
            return pagesLoaded * pagingInfo.ItemsPerPage.Value < paginationSet.TotalCount;
        }
    }
}
