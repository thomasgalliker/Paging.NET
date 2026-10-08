using System.Linq.Expressions;

namespace Paging.Queryable.Internals
{
    /// <summary>
    /// Holds a default sort registration which is applied
    /// when no sort order is requested (primary order)
    /// or appended as tie-breaker after the requested sort order.
    /// </summary>
    internal sealed class DefaultSortDefinition
    {
        private readonly Func<LambdaExpression>? keySelectorFactory;
        private readonly LambdaExpression? keySelector;

        internal DefaultSortDefinition(LambdaExpression keySelector, SortOrder sortOrder)
        {
            this.keySelector = keySelector;
            this.SortOrder = sortOrder;
        }

        internal DefaultSortDefinition(Func<LambdaExpression> keySelectorFactory, SortOrder sortOrder)
        {
            this.keySelectorFactory = keySelectorFactory;
            this.SortOrder = sortOrder;
        }

        internal SortOrder SortOrder { get; }

        internal LambdaExpression GetKeySelector()
        {
            return this.keySelector ?? this.keySelectorFactory!();
        }
    }
}
