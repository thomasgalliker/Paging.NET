using Paging.Internals;

namespace Paging
{
    /// <summary>
    /// PagingInfo is the paging specification used to instruct the data source
    /// on sorting, filtering and paging.
    /// </summary>
    public class PagingInfo : IEquatable<PagingInfo?>
    {
        public static readonly PagingInfo Default = new DefaultPagingInfo();

        private int currentPage;
        private int itemsPerPage;
        private IDictionary<string, object?> filter;

        public PagingInfo()
        {
            this.currentPage = 1;
            this.itemsPerPage = 0;
            this.filter = new Dictionary<string, object?>();
        }

        /// <summary>
        /// The currently selected page.
        /// Default CurrentPage = 1.
        /// </summary>
        public virtual int CurrentPage
        {
            get => this.currentPage;
            set => this.currentPage = value;
        }

        /// <summary>
        /// Number of items returned per page.
        /// Default ItemsPerPage = 0, which means, if ItemsPerPage is not specified,
        /// the request returns one page with all items.
        /// </summary>
        public virtual int ItemsPerPage
        {
            get => this.itemsPerPage;
            set => this.itemsPerPage = value;
        }

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
        public virtual string? SortBy { get; set; }

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
        public virtual IReadOnlyDictionary<string, SortOrder> Sorting
        {
            //TODO: Check if Sorting as wrapper for SortBy is practical (serialization/deserialization issues)
            get => this.SortBy.ToSorting();
            set => this.SortBy = value.ToSortByString();
        }

        /// <summary>
        /// The whole result list is reversed.
        /// </summary>
        public virtual bool Reverse { get; set; }

        /// <summary>
        /// Free-text which is used to search through the target collection of items.
        /// Search text is only used if a search predicated is specified.
        /// </summary>
        public virtual string? Search { get; set; }

        /// <summary>
        /// Property-based filtering. All specified {Key, Value} pairs are used to OR-filter
        /// the underlying collection.
        /// - Key is of type string and contains the property name of the property to be filtered.
        /// - Value is an arbitrary filter value (currently supported: string, decimal, DateTime).
        /// </summary>
        public virtual IDictionary<string, object?> Filter
        {
            get => this.filter;
            set => this.filter = value ?? new Dictionary<string, object?>();
        }

        public static bool operator ==(PagingInfo? left, PagingInfo? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(PagingInfo? left, PagingInfo? right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return this.ToQueryString();
        }

        public bool Equals(PagingInfo? other)
        {
            if (other is null)
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
                FilterEquals(this.Filter, other.Filter);
        }

        private static bool FilterEquals(IDictionary<string, object?> filter, IDictionary<string, object?> other)
        {
            if (filter is null || other is null)
            {
                return false;
            }

            if (ReferenceEquals(filter, other))
            {
                return true;
            }

            var sequenceEqual = filter.SequenceEqual(other);
            return sequenceEqual;
        }

        public override bool Equals(object? obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj is not PagingInfo p)
            {
                return false;
            }

            return this.Equals(p);
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

                if (this.Filter != null)
                {
                    foreach (var filter in this.Filter)
                    {
                        hashCode = (hashCode * 397) ^ filter.GetHashCode();
                    }
                }

                return hashCode;
            }
        }
    }
}
