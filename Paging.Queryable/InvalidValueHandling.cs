namespace Paging.Queryable
{
    /// <summary>
    /// Controls how filter values that cannot be applied to the target property are handled,
    /// e.g. values that fail type conversion (<c>Year == "not-a-number"</c>) or operators that
    /// cannot be built for the property type. Configured via
    /// <see cref="PagingOptions{TEntity}.InvalidFilterValues(InvalidValueHandling)"/>.
    /// </summary>
    /// <remarks>
    /// Semantically well-defined edge cases are never treated as invalid, regardless of this setting:
    /// an empty <c>in</c> list (matches nothing / everything when negated), null elements inside an
    /// <c>in</c> list (dropped; use <c>== null</c> to match nulls), an empty string needle
    /// (<c>contains ""</c> is skipped, its negation matches nothing) and custom filter predicate
    /// factories returning <c>null</c> (documented skip contract).
    /// </remarks>
    public enum InvalidValueHandling
    {
        /// <summary>
        /// The filter condition is silently skipped (traced via <see cref="System.Diagnostics.Trace"/>).
        /// This is the default. Note that a skipped condition widens the result set.
        /// </summary>
        Skip = 0,

        /// <summary>
        /// A <see cref="PagingException"/> carrying the external property name is thrown,
        /// surfacing the invalid request to the caller (e.g. as an HTTP 400).
        /// </summary>
        Throw = 1,
    }
}
