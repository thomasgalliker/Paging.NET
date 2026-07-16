using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Paging
{
    /// <summary>
    /// Base type for a node in a filter tree. A node is either a single
    /// <see cref="FilterCondition"/> or a <see cref="FilterGroup"/> combining
    /// multiple child nodes with <see cref="FilterLogic.And"/> or <see cref="FilterLogic.Or"/>.
    /// </summary>
    /// <remarks>
    /// The <see cref="TypeConverterAttribute"/> enables query-string model binding
    /// (e.g. ASP.NET Core <c>[FromQuery] PagingInfo</c> binds <c>?filter=...</c>);
    /// the <see cref="JsonConverterAttribute"/> covers JSON bodies. Both use the same
    /// compact filter expression string form. The static <see cref="TryParse(string?, out FilterNode?)"/>
    /// additionally satisfies the .NET 7+ TryParse binding convention.
    /// </remarks>
    [TypeConverter(typeof(FilterNodeTypeConverter))]
    [JsonConverter(typeof(FilterNodeJsonConverter))]
    public abstract class FilterNode : IEquatable<FilterNode?>
    {
        /// <summary>
        /// Parses a filter expression string (e.g. <c>Year &gt;= 2020 &amp;&amp; Name contains "bmw"</c>)
        /// into a <see cref="FilterNode"/> tree. Returns <c>null</c> for a null or whitespace expression.
        /// </summary>
        /// <exception cref="FormatException">The expression is syntactically invalid.</exception>
        [return: NotNullIfNotNull(nameof(expression))]
        public static FilterNode? Parse(string? expression)
        {
            return FilterExpressionParser.Parse(expression);
        }

        /// <summary>
        /// Attempts to parse a filter expression string into a <see cref="FilterNode"/> tree.
        /// </summary>
        public static bool TryParse(string? expression, out FilterNode? node)
        {
            try
            {
                node = FilterExpressionParser.Parse(expression);
                return true;
            }
            catch (FormatException)
            {
                node = null;
                return false;
            }
        }

        /// <summary>
        /// Implicitly converts a filter expression string into a <see cref="FilterNode"/> tree by parsing it,
        /// so a filter can be assigned directly: <c>pagingInfo.Filter = "Year &gt;= 2020";</c>.
        /// </summary>
        /// <exception cref="FormatException">The expression is syntactically invalid.</exception>
        [return: NotNullIfNotNull(nameof(expression))]
        public static implicit operator FilterNode?(string? expression)
        {
            return Parse(expression);
        }

        public abstract bool Equals(FilterNode? other);

        public override bool Equals(object? obj)
        {
            return this.Equals(obj as FilterNode);
        }

        public abstract override int GetHashCode();
    }
}
