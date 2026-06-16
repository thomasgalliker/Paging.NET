namespace Paging.Queryable.Internals
{
    /// <summary>
    /// The materialization mode derived from <see cref="PagingInfo.ItemsPerPage"/>.
    /// </summary>
    internal enum PageMode
    {
        /// <summary>Return the requested page (<c>ItemsPerPage &gt; 0</c>).</summary>
        Page,

        /// <summary>Return totals only and no items (<c>ItemsPerPage == 0</c>).</summary>
        TotalsOnly,

        /// <summary>Return all matching items (<c>ItemsPerPage == null</c>).</summary>
        All,
    }

    /// <summary>
    /// Shared paging calculations used by the synchronous extensions and the EF Core async extensions.
    /// </summary>
    internal static class PagingMath
    {
        internal static PageMode GetPageMode(PagingInfo pagingInfo)
        {
            if (pagingInfo.ItemsPerPage > 0)
            {
                return PageMode.Page;
            }

            return pagingInfo.ItemsPerPage == 0 ? PageMode.TotalsOnly : PageMode.All;
        }

        internal static int GetSkip(PagingInfo pagingInfo)
        {
            return (pagingInfo.CurrentPage - pagingInfo.FirstPageIndex) * pagingInfo.ItemsPerPage!.Value;
        }

        internal static int GetTake(PagingInfo pagingInfo)
        {
            return pagingInfo.ItemsPerPage!.Value;
        }
    }
}
