using System;
using System.Collections.Generic;
using System.Linq;

namespace Paging
{
    internal static class PagingInfoExtensions
    {
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
