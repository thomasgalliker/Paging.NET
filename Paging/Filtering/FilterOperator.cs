namespace Paging
{
    /// <summary>
    /// The comparison applied by a <see cref="FilterCondition"/>.
    /// The operator is resolved on the server against a property declared by the backend;
    /// it never carries an expression tree over the wire.
    /// </summary>
    public enum FilterOperator
    {
        /// <summary>Property equals the filter value.</summary>
        Equal = 0,

        /// <summary>Property does not equal the filter value.</summary>
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

        /// <summary>Property matches any of the values in the filter value collection (IN filter).</summary>
        In = 9,
    }
}
