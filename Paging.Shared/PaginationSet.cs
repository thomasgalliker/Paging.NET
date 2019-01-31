using System;
using System.Collections.Generic;
using System.Linq;

namespace Paging
{
    public class PaginationSet<T>
    {
        public PaginationSet() : this(new HashSet<T>())
        {
        }

        /// <summary>
        /// Creates a new PaginationSet with a given collection of <typeparamref name="T"/>.
        /// Since no <seealso cref="PagingInfo"/> is specified, a single-page request is assumed.
        /// </summary>
        /// <param name="items">The collection of items.</param>
        public PaginationSet(IEnumerable<T> items) : this(PagingInfo.Default, items, items.Count(), items.Count())
        {
        }

        /// <summary>
        /// Creates a new PaginationSet.
        /// </summary>
        /// <param name="pagingInfo">The original paging request.</param>
        /// <param name="items">The collection of items for the current paging request.</param>
        /// <param name="totalCount">Total number of items available that match with the filter/search.</param>
        /// <param name="totalCountUnfiltered">Total number of items (if no filter/search applied).</param>
        public PaginationSet(PagingInfo pagingInfo, IEnumerable<T> items, int totalCount, int totalCountUnfiltered)
        {
            pagingInfo = pagingInfo ?? PagingInfo.Default;
            this.CurrentPage = pagingInfo.CurrentPage;
            this.Items = items ?? Enumerable.Empty<T>();
            if (pagingInfo.ItemsPerPage > 0)
            {
                this.TotalPages = (int)Math.Ceiling((decimal)totalCount / pagingInfo.ItemsPerPage);
            }
            else
            {
                this.TotalPages = 1;
            }

            this.TotalCount = totalCount;
            this.TotalCountUnfiltered = totalCountUnfiltered;
        }

        /// <summary>
        /// The page index of the currently filtered set.
        /// CurrentPage is 1-indexed, means the first page is CurrentPage = 1 (not CurrentPage = 0).
        /// </summary>
        public int CurrentPage { get; set; }

        /// <summary>
        /// The total number of pages that match the filter/search criteria.
        /// TotalPages = TotalCount / ItemsPerPage.
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// The total number of items which match the filter/search criteria.
        /// If ItemsPerPage is set to 0, Items.Count equals to TotalCount.
        /// This means, a single page is returned containing all available items which match the filter/search criteria.
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// The total number of items if no filter/search is applied.
        /// </summary>
        public int TotalCountUnfiltered { get; set; }

        /// <summary>
        /// The paged collection of items which match the filter/search criteria.
        /// If ItemsPerPage is set >0, ItemsPerPage equals to Items.Count.
        /// </summary>
        public IEnumerable<T> Items { get; set; }

        /// <summary>
        /// Checks if there are more pages available after the current pagination set.
        /// </summary>
        /// <returns><c>true</c> if TotalPages > CurrentPage, otherwise <c>false</c>.</returns>
        public bool HasMorePages()
        {
            return this.TotalPages > this.CurrentPage;
        }
    }
}
