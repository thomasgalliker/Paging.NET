using System.Collections;

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
                   ValueEquals(this.Value, condition.Value);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = this.Property != null ? this.Property.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (int)this.Operator;
                hashCode = (hashCode * 397) ^ ValueHashCode(this.Value);
                return hashCode;
            }
        }

        // List values (IN filters) compare element-wise and order-sensitively, so two conditions
        // with equal-content lists are equal regardless of the collection instance/type.
        // Note: element types must match exactly (1 (int) != 1L (long)); the expression parser
        // always produces long/double, so parse-to-parse comparisons are stable.
        private static bool ValueEquals(object? left, object? right)
        {
            if (left is IEnumerable leftEnumerable && right is IEnumerable rightEnumerable &&
                left is not string && right is not string)
            {
                var leftIterator = leftEnumerable.GetEnumerator();
                var rightIterator = rightEnumerable.GetEnumerator();

                while (true)
                {
                    var leftHasNext = leftIterator.MoveNext();
                    var rightHasNext = rightIterator.MoveNext();

                    if (leftHasNext != rightHasNext)
                    {
                        return false;
                    }

                    if (!leftHasNext)
                    {
                        return true;
                    }

                    if (!ValueEquals(leftIterator.Current, rightIterator.Current))
                    {
                        return false;
                    }
                }
            }

            return Equals(left, right);
        }

        private static int ValueHashCode(object? value)
        {
            if (value is IEnumerable enumerable && value is not string)
            {
                unchecked
                {
                    var hashCode = 397;
                    foreach (var item in enumerable)
                    {
                        hashCode = (hashCode * 397) ^ ValueHashCode(item);
                    }

                    return hashCode;
                }
            }

            return value != null ? value.GetHashCode() : 0;
        }

        public override string ToString()
        {
            return FilterExpressionWriter.Write(this);
        }
    }
}
