namespace Paging
{
    internal static class PaginationSetExtensions
    {
        internal static bool StopScroll<T>(this PaginationSet<T> paginationSet, PagingInfo pagingInfo)
        {
            if (paginationSet == null)
            {
                return false;
            }

            var stopScroll = pagingInfo.ItemsPerPage * pagingInfo.CurrentPage >= paginationSet.TotalCount;

            return stopScroll;
        }
    }
}