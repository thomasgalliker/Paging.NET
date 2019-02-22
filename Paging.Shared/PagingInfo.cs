using System;
using System.Collections.Generic;
using System.Linq;

namespace Paging
{
    /// <summary>
    /// PagingInfo is the paging specification used to instruct the data source
    /// on sorting, filtering and paging.
    /// </summary>
    public class PagingInfo : IEquatable<PagingInfo>
    {
        public static readonly PagingInfo Default = new PagingInfo();

        public PagingInfo()
        {
            this.CurrentPage = 1;
            this.ItemsPerPage = 0;
            this.Filter = new Dictionary<string, object>();
        }

        /// <summary>
        /// The currently selected page.
        /// Default CurrentPage = 1.
        /// </summary>
        public int CurrentPage { get; set; }

        /// <summary>
        /// Number of items returned per page.
        /// Default ItemsPerPage = 0, which means, if ItemsPerPage is not specified,
        /// the request returns one page with all items.
        /// </summary>
        public int ItemsPerPage { get; set; }

        /// <summary>
        /// SortBy is a comma-separated sort specification.
        /// Use either <seealso cref="Sorting" /> or <seealso cref="SortBy" /> to specify single- or multi-property sort
        /// orders.
        /// </summary>
        /// <example>
        /// Sorting a single property in ascending order:
        /// SortBy = "property1"
        /// SortBy = "property1 ascending"
        /// 
        /// Sorting a multiple properties with mixed ordering:
        /// SortBy = "property1 descending, property2 ascending"
        /// </example>
        public string SortBy { get; set; }

        /// <summary>
        /// Property-based sort specification.
        /// Use either <seealso cref="Sorting" /> or <seealso cref="SortBy" /> to specify single- or multi-property sort
        /// orders.
        /// </summary>
        /// <example>
        /// Sorting a single property in ascending order:
        /// Sorting = {{"property1", SortOrder.Asc}}
        /// Sorting a multiple properties with mixed ordering:
        /// Sorting = {
        ///     {"property1", SortOrder.Desc},
        ///     {"property2", SortOrder.Asc}
        /// }
        /// </example>
        public IReadOnlyDictionary<string, SortOrder> Sorting
        {
            //TODO: Check if Sorting as wrapper for SortBy is practical (serialization/deserialization issues)
            get => this.SortBy.ToSorting();
            set => this.SortBy = value.ToSortByString();
        }

        /// <summary>
        /// The whole result list is reversed.
        /// </summary>
        public bool Reverse { get; set; }

        /// <summary>
        /// Free-text which is used to search trough the target collection of items.
        /// Search text is only used if a search predicated is specified.
        /// </summary>
        public string Search { get; set; }

        /// <summary>
        /// Property-based filtering. All specified {Key, Value} pairs are used to OR-filter
        /// the underlying collection.
        /// - Key is of type string and contains the property name of the property to be filtered.
        /// - Value is an arbitrary filter value (currently supported: string, decimal, DateTime).
        /// </summary>
        public IDictionary<string, object> Filter { get; set; }

        public static bool operator ==(PagingInfo pi1, PagingInfo pi2)
        {
            return Equals(pi1, pi2);
        }

        public static bool operator !=(PagingInfo pi1, PagingInfo pi2)
        {
            return !(pi1 == pi2);
        }

        public override string ToString()
        {
            return this.ToQueryString();
        }

        public bool Equals(PagingInfo other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return
                   this.CurrentPage == other.CurrentPage &&
                   this.ItemsPerPage == other.ItemsPerPage &&
                   string.Equals(this.SortBy, other.SortBy) &&
                   this.Reverse == other.Reverse &&
                   string.Equals(this.Search, other.Search) &&
                   Equals(Filter, other.Filter);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }
            return this.Equals((PagingInfo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = this.CurrentPage;
                hashCode = (hashCode * 397) ^ this.ItemsPerPage;
                hashCode = (hashCode * 397) ^ (this.SortBy != null ? this.SortBy.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ this.Reverse.GetHashCode();
                hashCode = (hashCode * 397) ^ (this.Search != null ? this.Search.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (this.Filter != null ? this.Filter.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    public enum SortOrder
    {
        Asc,
        Desc,
    }
}
