using System.Linq.Expressions;
using System.Reflection;

namespace Paging.Queryable.Internals
{
    internal static class SortExpressionBuilder
    {
        /// <summary>
        /// Creates a lambda expression <c>e =&gt; e.Property.SubProperty</c> from a dotted property path.
        /// Path segments are resolved case-insensitively.
        /// Returns <c>null</c> if the path cannot be resolved on <typeparamref name="TEntity"/>.
        /// </summary>
        internal static LambdaExpression? CreatePropertyPathLambda<TEntity>(string propertyPath)
        {
            if (string.IsNullOrWhiteSpace(propertyPath))
            {
                return null;
            }

            var parameter = Expression.Parameter(typeof(TEntity), "e");
            Expression body = parameter;

            foreach (var segment in propertyPath.Split('.'))
            {
                var propertyInfo = body.Type.GetProperty(
                    segment,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                if (propertyInfo == null)
                {
                    return null;
                }

                body = Expression.Property(body, propertyInfo);
            }

            return Expression.Lambda(body, parameter);
        }

        /// <summary>
        /// Extracts the dotted property path from a pure property access chain,
        /// e.g. <c>e =&gt; e.Owner.Name</c> returns <c>"Owner.Name"</c>.
        /// Returns <c>null</c> if the lambda body is not a property access chain.
        /// </summary>
        internal static string? GetPropertyPath(LambdaExpression lambda)
        {
            var expression = lambda.Body;

            while (expression is UnaryExpression unaryExpression &&
                   (unaryExpression.NodeType == ExpressionType.Convert || unaryExpression.NodeType == ExpressionType.ConvertChecked))
            {
                expression = unaryExpression.Operand;
            }

            var segments = new List<string>();

            while (expression is MemberExpression memberExpression && memberExpression.Member is PropertyInfo)
            {
                segments.Insert(0, memberExpression.Member.Name);
                expression = memberExpression.Expression;
            }

            if (expression is ParameterExpression && segments.Count > 0)
            {
                return string.Join(".", segments);
            }

            return null;
        }

        /// <summary>
        /// Appends an OrderBy/OrderByDescending (first sort) or ThenBy/ThenByDescending (subsequent sorts)
        /// call for the given key selector to the query. The key selector remains an expression tree,
        /// so query providers such as EF Core can translate it to SQL.
        /// </summary>
        internal static IQueryable<TEntity> ApplyOrderBy<TEntity>(
            IQueryable<TEntity> queryable,
            LambdaExpression keySelector,
            SortOrder sortOrder,
            bool isFirst)
        {
            var methodName = isFirst
                ? (sortOrder == SortOrder.Asc ? nameof(System.Linq.Queryable.OrderBy) : nameof(System.Linq.Queryable.OrderByDescending))
                : (sortOrder == SortOrder.Asc ? nameof(System.Linq.Queryable.ThenBy) : nameof(System.Linq.Queryable.ThenByDescending));

            var methodCallExpression = Expression.Call(
                typeof(System.Linq.Queryable),
                methodName,
                new[] { typeof(TEntity), keySelector.ReturnType },
                queryable.Expression,
                Expression.Quote(keySelector));

            return queryable.Provider.CreateQuery<TEntity>(methodCallExpression);
        }
    }
}
