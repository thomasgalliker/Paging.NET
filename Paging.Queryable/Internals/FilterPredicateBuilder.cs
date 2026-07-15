using System.Diagnostics;
using System.Linq.Expressions;

namespace Paging.Queryable.Internals
{
    /// <summary>
    /// Walks a <see cref="FilterNode"/> tree and builds a single predicate expression.
    /// Each <see cref="FilterCondition"/> is resolved against the properties declared in
    /// <see cref="PagingOptions{TEntity}"/> (allow-list); unknown property names are handled
    /// according to <see cref="PagingOptions{TEntity}.UnknownFilterPropertyHandling"/>.
    /// </summary>
    internal static class FilterPredicateBuilder
    {
        internal static Expression<Func<TEntity, bool>>? Build<TEntity>(FilterNode? node, PagingOptions<TEntity> pagingOptions)
        {
            if (node == null)
            {
                return null;
            }

            return node switch
            {
                FilterGroup group => BuildGroup(group, pagingOptions),
                FilterCondition condition => BuildCondition(condition, pagingOptions),
                _ => throw new PagingException($"Unsupported filter node type '{node.GetType().Name}'.", null),
            };
        }

        private static Expression<Func<TEntity, bool>>? BuildGroup<TEntity>(FilterGroup group, PagingOptions<TEntity> pagingOptions)
        {
            Expression<Func<TEntity, bool>>? combined = null;

            foreach (var childNode in group.Nodes)
            {
                var childPredicate = Build(childNode, pagingOptions);
                if (childPredicate == null)
                {
                    continue;
                }

                combined = combined == null
                    ? childPredicate
                    : PredicateComposer.Combine(combined, childPredicate, group.Logic);
            }

            return combined;
        }

        private static Expression<Func<TEntity, bool>>? BuildCondition<TEntity>(FilterCondition condition, PagingOptions<TEntity> pagingOptions)
        {
            if (string.IsNullOrEmpty(condition.Property))
            {
                throw new PagingException("Filter condition must specify a property name.", condition.Property);
            }

            if (pagingOptions.TryGetFilterDefinition(condition.Property, out var filterDefinition))
            {
                if (filterDefinition.IsCollectionFilter)
                {
                    return FilterExpressionBuilder.BuildCollectionAnyPredicate<TEntity>(
                        filterDefinition.CollectionSelector!,
                        filterDefinition.ElementSelector!,
                        filterDefinition.PropertyPath,
                        condition.Operator,
                        condition.Value);
                }

                if (filterDefinition.HasCustomFilter)
                {
                    var customPredicate = filterDefinition.BuildCustomPredicate(condition.Operator, condition.Value);
                    if (customPredicate != null)
                    {
                        Trace.WriteLine($"Paging.ApplyFilter: Custom filter predicate for property '{condition.Property}'");
                    }

                    return customPredicate;
                }

                var propertyLambda = filterDefinition.GetFilterPropertyLambda();

                return FilterExpressionBuilder.BuildPredicate<TEntity>(propertyLambda, filterDefinition.PropertyPath, condition.Operator, condition.Value);
            }

            switch (pagingOptions.UnknownFilterPropertyHandling)
            {
                case UnknownPropertyHandling.Ignore:
                    Trace.WriteLine($"Paging.ApplyFilter: Filter property '{condition.Property}' is ignored (not registered).");
                    return null;

                case UnknownPropertyHandling.Throw:
                default:
                    throw new PagingException($"Filter property '{condition.Property}' is not allowed.", condition.Property);
            }
        }
    }
}
