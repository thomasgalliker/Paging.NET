using System;
using System.Collections.Generic;
using System.Linq;

namespace Paging
{
    public class PaginationSet<T>
    {
        public PaginationSet()
        {
            this.Items = new HashSet<T>();
        }

        public PaginationSet(IEnumerable<T> items)
        {
            this.CurrentPage = 1;
            this.Items = items;
            this.TotalPages = 1;
            this.TotalCount = this.TotalCountUnfiltered = this.Items.Count();
        }

        public PaginationSet(PagingInfo pagingInfo, IEnumerable<T> items, int totalCount, int totalCountUnfiltered)
        {
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
        /// </summary>
        public int CurrentPage { get; set; }

        /// <summary>
        /// The total number of pages to be returned if the ItemsPerPage parameter remains constant.
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// The total number of items to be returned.
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// The total number of items if no filter was applied.
        /// </summary>
        public int TotalCountUnfiltered { get; set; }

        /// <summary>
        /// The list of filtered items.
        /// </summary>
        public IEnumerable<T> Items { get; set; }
    }
}