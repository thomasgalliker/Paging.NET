using System.Text.Json.Serialization;

namespace Paging
{
    [JsonConverter(typeof(PaginationSetJsonConverterFactory))]
    public class PaginationSet<T>
    {
        public PaginationSet()
            : this(new HashSet<T>())
        {
        }

        /// <summary>
        /// Creates a new PaginationSet with a given collection of <typeparamref name="T"/>.
        /// Since no <seealso cref="PagingInfo"/> is specified, a single-page request is assumed.
        /// </summary>
        /// <param name="items">The collection of items.</param>
        public PaginationSet(IEnumerable<T> items)
            : this(new PagingInfo(), items, items.Count(), items.Count())
        {
        }

        /// <summary>
        /// Creates a new PaginationSet.
        /// </summary>
        /// <param name="pagingInfo">The original paging request.</param>
        /// <param name="items">The collection of items for the current paging request.</param>
        /// <param name="totalCount">Total number of items available that match with the filter/search.</param>
        /// <param name="totalCountUnfiltered">Total number of items (if no filter/search applied).</param>
        public PaginationSet(PagingInfo? pagingInfo, IEnumerable<T> items, int totalCount, int totalCountUnfiltered)
        {
            pagingInfo ??= new PagingInfo();
            this.FirstPageIndex = pagingInfo.FirstPageIndex;
            this.CurrentPage = pagingInfo.CurrentPage;
            this.Items = items ?? Enumerable.Empty<T>();
            if (pagingInfo.ItemsPerPage > 0)
            {
                this.TotalPages = (int)Math.Ceiling((double)totalCount / pagingInfo.ItemsPerPage.Value);
            }
            else if (pagingInfo.ItemsPerPage == 0)
            {
                this.TotalPages = 0;
            }
            else
            {
                this.TotalPages = 1;
            }

            this.TotalCount = totalCount;
            this.TotalCountUnfiltered = totalCountUnfiltered;
        }

        /// <summary>
        /// The first valid page index for this result set.
        /// </summary>
        [JsonPropertyName("firstPageIndex")]
        public int FirstPageIndex { get; set; }

        /// <summary>
        /// The page index of the currently filtered set.
        /// CurrentPage is relative to the request's PagingInfo.FirstPageIndex.
        /// </summary>
        [JsonPropertyName("currentPage")]
        public int CurrentPage { get; set; }

        /// <summary>
        /// The total number of pages that match the filter/search criteria.
        /// TotalPages = TotalCount / ItemsPerPage.
        /// </summary>
        [JsonPropertyName("totalPages")]
        public int TotalPages { get; set; }

        /// <summary>
        /// The total number of items which match the filter/search criteria.
        /// If ItemsPerPage is set to 0, the request only returns totals and Items is empty.
        /// If ItemsPerPage is null, all matching items are returned in a single unpaged result.
        /// </summary>
        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }

        /// <summary>
        /// The total number of items if no filter/search is applied.
        /// </summary>
        [JsonPropertyName("totalCountUnfiltered")]
        public int TotalCountUnfiltered { get; set; }

        /// <summary>
        /// The paged collection of items which match the filter/search criteria.
        /// If ItemsPerPage is set >0, Items.Count contains the requested page items.
        /// </summary>
        [JsonPropertyName("items")]
        public IEnumerable<T> Items { get; set; }

        /// <summary>
        /// Checks if there are more pages available after the current pagination set.
        /// </summary>
        /// <returns><c>true</c> if TotalPages > CurrentPage, otherwise <c>false</c>.</returns>
        public bool HasMorePages()
        {
            if (this.TotalPages <= 0)
            {
                return false;
            }

            return this.CurrentPage < this.FirstPageIndex + this.TotalPages - 1;
        }
    }
}
