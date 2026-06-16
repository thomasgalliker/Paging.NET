using System.Linq.Expressions;

namespace Paging.Queryable.Internals
{
    /// <summary>
    /// Combines two predicate expressions with a logical AND/OR while rebinding their
    /// parameters onto a single shared parameter, so the resulting predicate remains a
    /// pure expression tree translatable by query providers such as EF Core.
    /// </summary>
    internal static class PredicateComposer
    {
        internal static Expression<Func<TEntity, bool>> Combine<TEntity>(Expression<Func<TEntity, bool>> left, Expression<Func<TEntity, bool>> right, FilterLogic logic)
        {
            var parameter = Expression.Parameter(typeof(TEntity), "e");

            var leftBody = new ParameterRebinder(left.Parameters[0], parameter).Visit(left.Body);
            var rightBody = new ParameterRebinder(right.Parameters[0], parameter).Visit(right.Body);

            var body = logic == FilterLogic.Or
                ? Expression.OrElse(leftBody, rightBody)
                : Expression.AndAlso(leftBody, rightBody);

            return Expression.Lambda<Func<TEntity, bool>>(body, parameter);
        }

        private sealed class ParameterRebinder : ExpressionVisitor
        {
            private readonly ParameterExpression source;
            private readonly ParameterExpression target;

            internal ParameterRebinder(ParameterExpression source, ParameterExpression target)
            {
                this.source = source;
                this.target = target;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return node == this.source ? this.target : base.VisitParameter(node);
            }
        }
    }
}
