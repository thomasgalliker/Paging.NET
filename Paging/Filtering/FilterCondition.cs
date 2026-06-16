namespace Paging
{
    /// <summary>
    /// A single filter condition: a backend-declared property name, a comparison
    /// <see cref="FilterOperator"/> and a value. The property name is resolved on the
    /// server against the registered <c>PagingOptions</c> properties.
    /// </summary>
    public sealed class FilterCondition : FilterNode
    {
        public FilterCondition()
        {
            this.Property = string.Empty;
        }

        public FilterCondition(string property, FilterOperator @operator, object? value)
        {
            this.Property = property ?? throw new ArgumentNullException(nameof(property));
            this.Operator = @operator;
            this.Value = value;
        }

        /// <summary>
        /// The external (client-facing) property name to filter on.
        /// </summary>
        public string Property { get; set; }

        /// <summary>
        /// The comparison applied between the property and <see cref="Value"/>.
        /// </summary>
        public FilterOperator Operator { get; set; }

        /// <summary>
        /// The filter value. For <see cref="FilterOperator.In"/> this is a collection of values.
        /// </summary>
        public object? Value { get; set; }

        public override bool Equals(FilterNode? other)
        {
            return other is FilterCondition condition &&
                   string.Equals(this.Property, condition.Property) &&
                   this.Operator == condition.Operator &&
                   Equals(this.Value, condition.Value);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = this.Property != null ? this.Property.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (int)this.Operator;
                hashCode = (hashCode * 397) ^ (this.Value != null ? this.Value.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return FilterExpressionWriter.Write(this);
        }
    }
}
