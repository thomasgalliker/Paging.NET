namespace Paging
{
    public static class PaginationSetExtensions
    {
        /// <summary>
        /// Determines whether infinite scrolling should stop because all available items have been loaded.
        /// </summary>
        /// <typeparam name="T">The item type contained in the pagination set.</typeparam>
        /// <param name="paginationSet">The current pagination set.</param>
        /// <param name="pagingInfo">The paging configuration used to load items.</param>
        /// <returns>
        /// <see langword="true"/> when no more items should be loaded; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool StopScroll<T>(this PaginationSet<T>? paginationSet, PagingInfo pagingInfo)
        {
            if (paginationSet == null)
            {
                return false;
            }

            if (pagingInfo == null)
            {
                throw new ArgumentNullException(nameof(pagingInfo));
            }

            var stopScroll = pagingInfo.ItemsPerPage * pagingInfo.CurrentPage >= paginationSet.TotalCount;
            return stopScroll;
        }
    }
}