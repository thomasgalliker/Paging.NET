namespace Paging
{
    /// <summary>
    /// Maps <see cref="FilterOperator"/> values to and from their textual tokens
    /// used in the filter expression string (e.g. <c>"Year &gt;= 2020 &amp;&amp; Name contains \"bmw\""</c>).
    /// </summary>
    internal static class FilterOperatorTokens
    {
        internal static string ToToken(FilterOperator filterOperator)
        {
            return filterOperator switch
            {
                FilterOperator.Equal => "==",
                FilterOperator.NotEqual => "!=",
                FilterOperator.GreaterThan => ">",
                FilterOperator.GreaterThanOrEqual => ">=",
                FilterOperator.LessThan => "<",
                FilterOperator.LessThanOrEqual => "<=",
                FilterOperator.Contains => "contains",
                FilterOperator.StartsWith => "startswith",
                FilterOperator.EndsWith => "endswith",
                FilterOperator.In => "in",
                _ => throw new ArgumentOutOfRangeException(nameof(filterOperator), filterOperator, "Unknown filter operator."),
            };
        }

        internal static bool TryParseKeyword(string word, out FilterOperator filterOperator)
        {
            switch (word.ToLowerInvariant())
            {
                case "contains":
                    filterOperator = FilterOperator.Contains;
                    return true;
                case "startswith":
                    filterOperator = FilterOperator.StartsWith;
                    return true;
                case "endswith":
                    filterOperator = FilterOperator.EndsWith;
                    return true;
                case "in":
                    filterOperator = FilterOperator.In;
                    return true;
                default:
                    filterOperator = default;
                    return false;
            }
        }

        internal static bool IsKeyword(string word)
        {
            return TryParseKeyword(word, out _);
        }
    }
}
