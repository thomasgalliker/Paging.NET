namespace Paging
{
    /// <summary>
    /// The comparison applied by a <see cref="FilterCondition"/>.
    /// The operator is resolved on the server against a property declared by the backend;
    /// it never carries an expression tree over the wire.
    /// Equality (<see cref="Equal"/>/<see cref="NotEqual"/>), the <see cref="In"/>/<see cref="NotIn"/> filters
    /// and the string operators are case-insensitive for strings; the ordering comparisons
    /// (<see cref="GreaterThan"/>/<see cref="GreaterThanOrEqual"/>/<see cref="LessThan"/>/<see cref="LessThanOrEqual"/>)
    /// are not.
    /// </summary>
    public enum FilterOperator
    {
        /// <summary>Property equals the filter value (case-insensitive for strings).</summary>
        Equal = 0,

        /// <summary>Property does not equal the filter value (case-insensitive for strings).</summary>
        NotEqual = 1,

        /// <summary>Property is greater than the filter value.</summary>
        GreaterThan = 2,

        /// <summary>Property is greater than or equal to the filter value.</summary>
        GreaterThanOrEqual = 3,

        /// <summary>Property is less than the filter value.</summary>
        LessThan = 4,

        /// <summary>Property is less than or equal to the filter value.</summary>
        LessThanOrEqual = 5,

        /// <summary>Property contains the filter value (case-insensitive substring match).</summary>
        Contains = 6,

        /// <summary>Property starts with the filter value (case-insensitive).</summary>
        StartsWith = 7,

        /// <summary>Property ends with the filter value (case-insensitive).</summary>
        EndsWith = 8,

        /// <summary>Property matches any of the values in the filter value collection (IN filter, case-insensitive for strings).</summary>
        In = 9,

        /// <summary>Property does not contain the filter value (negated case-insensitive substring match).</summary>
        NotContains = 10,

        /// <summary>Property does not start with the filter value (negated case-insensitive).</summary>
        NotStartsWith = 11,

        /// <summary>Property does not end with the filter value (negated case-insensitive).</summary>
        NotEndsWith = 12,

        /// <summary>Property matches none of the values in the filter value collection (negated IN filter, case-insensitive for strings).</summary>
        NotIn = 13,
    }
}
