namespace Paging
{
    public static class PaginationSetExtensions
    {
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