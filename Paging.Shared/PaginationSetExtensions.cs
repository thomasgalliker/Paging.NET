namespace Paging
{
    public static class PaginationSetExtensions
    {
        public static bool StopScroll<T>(this PaginationSet<T> paginationSet, PagingInfo pagingInfo)
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