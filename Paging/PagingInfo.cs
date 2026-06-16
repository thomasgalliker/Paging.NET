using System.Text.Json.Serialization;
using Paging.Internals;

namespace Paging
{
    /// <summary>
    /// PagingInfo is the paging specification used to instruct the data source
    /// on sorting, filtering and paging.
    /// </summary>
    [JsonConverter(typeof(PagingInfoJsonConverter))]
    public class PagingInfo : IEquatable<PagingInfo?>
    {
        private int firstPageIndex;
        private int currentPage;
        private int? itemsPerPage;

        /// <summary>
        /// Gets a read-only snapshot of the current library defaults.
        /// </summary>
        public static PagingInfo Default => new DefaultPagingInfo();

        public PagingInfo()
        {
            this.firstPageIndex = DefaultFirstPageIndex;
            this.currentPage = this.firstPageIndex;
            this.itemsPerPage = DefaultItemsPerPage;
        }

        /// <summary>
        /// Gets or sets the library default first page index used when no explicit request value is provided.
        /// Allowed values are <c>0</c> and <c>1</c> (default).
        /// </summary>
        public static int DefaultFirstPageIndex
        {
            get;
            set
            {
                ValidateFirstPageIndex(value, nameof(value));
                field = value;
            }
        } = 1;

        /// <summary>
        /// Gets or sets the first valid page index for this request.
        /// Allowed values are <c>0</c> and <c>1</c> (default).
        /// If the current request points to the first page, changing this value keeps <see cref="CurrentPage"/>
        /// aligned with the new first page index.
        /// </summary>
        [JsonPropertyName("firstPageIndex")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public virtual int FirstPageIndex
        {
            get => this.firstPageIndex;
            set
            {
                ValidateFirstPageIndex(value, nameof(value));

                // Keep requests that currently point to the first page aligned with the new first page index,
                // and clamp any now-invalid CurrentPage value to the new minimum.
                var wasAtPreviousFirstPage = this.currentPage == this.firstPageIndex;
                this.firstPageIndex = value;

                if (wasAtPreviousFirstPage || this.currentPage < this.firstPageIndex)
                {
                    this.currentPage = this.firstPageIndex;
                }
            }
        }

        /// <summary>
        /// The currently selected page.
        /// Defaults to <see cref="FirstPageIndex"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the value is smaller than <see cref="FirstPageIndex"/>.
        /// </exception>
        [JsonPropertyName("currentPage")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public virtual int CurrentPage
        {
            get => this.currentPage;
            set
            {
                if (value < this.FirstPageIndex)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), $"CurrentPage must be greater than or equal to {this.FirstPageIndex}.");
                }

                this.currentPage = value;
            }
        }

        /// <summary>
        /// Gets or sets the global default page size for new <see cref="PagingInfo"/> instances.
        /// The default value is <c>null</c>.
        /// <list type="bullet">
        /// <item>
        /// <description><c>null</c> disables paging and returns all matching items.</description>
        /// </item>
        /// <item>
        /// <description><c>0</c> returns totals only.</description>
        /// </item>
        /// <item>
        /// <description>Values greater than <c>0</c> enable regular paging.</description>
        /// </item>
        /// </list>
        /// </summary>
        public static int? DefaultItemsPerPage
        {
            get;
            set
            {
                ValidateItemsPerPage(value, nameof(value));
                field = value;
            }
        }

        /// <summary>
        /// Number of items returned per page.
        /// <list type="bullet">
        /// <item>
        /// <description><c>null</c> disables paging and returns all matching items (default).</description>
        /// </item>
        /// <item>
        /// <description><c>0</c> returns totals only and no items.</description>
        /// </item>
        /// <item>
        /// <description>Values greater than <c>0</c> enable regular paging.</description>
        /// </item>
        /// </list>
        /// </summary>
        [JsonPropertyName("itemsPerPage")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public virtual int? ItemsPerPage
        {
            get => this.itemsPerPage;
            set
            {
                ValidateItemsPerPage(value, nameof(value));
                this.itemsPerPage = value;
            }
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
        [JsonPropertyName("sortBy")]
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
        [JsonPropertyName("sorting")]
        public virtual IReadOnlyDictionary<string, SortOrder> Sorting
        {
            //TODO: Check if Sorting as wrapper for SortBy is practical (serialization/deserialization issues)
            get => this.SortBy.ToSorting();
            set => this.SortBy = value.ToSortByString();
        }

        /// <summary>
        /// The whole result list is reversed.
        /// </summary>
        [JsonPropertyName("reverse")]
        public virtual bool Reverse { get; set; }

        /// <summary>
        /// Free-text which is used to search through the target collection of items.
        /// Search text is only used if a search predicated is specified.
        /// </summary>
        [JsonPropertyName("search")]
        public virtual string? Search { get; set; }

        /// <summary>
        /// Property-based filtering expressed as a tree of <see cref="FilterNode"/>s.
        /// The root is either a single <see cref="FilterCondition"/> or a <see cref="FilterGroup"/>
        /// combining child nodes with <see cref="FilterLogic.And"/> or <see cref="FilterLogic.Or"/>.
        /// Each condition references a property name declared by the backend; only the property name,
        /// operator and value travel over the wire, never an expression tree.
        /// <c>null</c> means no filtering is applied.
        /// </summary>
        [JsonPropertyName("filter")]
        public virtual FilterNode? Filter { get; set; }

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
                this.FirstPageIndex == other.FirstPageIndex &&
                this.CurrentPage == other.CurrentPage &&
                this.ItemsPerPage == other.ItemsPerPage &&
                string.Equals(this.SortBy, other.SortBy) &&
                this.Reverse == other.Reverse &&
                string.Equals(this.Search, other.Search) &&
                Equals(this.Filter, other.Filter);
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
                var hashCode = this.FirstPageIndex;
                hashCode = (hashCode * 397) ^ this.CurrentPage;
                hashCode = (hashCode * 397) ^ this.ItemsPerPage.GetHashCode();
                hashCode = (hashCode * 397) ^ (this.SortBy != null ? this.SortBy.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ this.Reverse.GetHashCode();
                hashCode = (hashCode * 397) ^ (this.Search != null ? this.Search.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (this.Filter != null ? this.Filter.GetHashCode() : 0);
                return hashCode;
            }
        }

        private static void ValidateItemsPerPage(int? value, string paramName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(paramName, "ItemsPerPage must be null, 0, or a positive number.");
            }
        }

        private static void ValidateFirstPageIndex(int value, string paramName)
        {
            if (value is not 0 and not 1)
            {
                throw new ArgumentOutOfRangeException(paramName, "FirstPageIndex must be either 0 or 1.");
            }
        }
    }
}
