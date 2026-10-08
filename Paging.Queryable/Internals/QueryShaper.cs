using System.Diagnostics;

namespace Paging.Queryable.Internals
{
    /// <summary>
    /// Applies the search, filter and sort defined by a <see cref="PagingInfo"/> request to a query,
    /// returning the shaped <see cref="IQueryable{T}"/> without paging (no <c>Skip</c>/<c>Take</c>)
    /// and without materializing. Counting and paging are performed by the terminal extension methods.
    /// </summary>
    internal static class QueryShaper
    {
        internal static IQueryable<TEntity> Shape<TEntity>(IQueryable<TEntity> queryable, PagingInfo? pagingInfo, PagingOptions<TEntity> pagingOptions)
        {
            if (pagingInfo == null)
            {
                return ApplyDefaultSort(queryable, pagingOptions, appliedSortCount: 0);
            }

            queryable = ApplySearch(queryable, pagingInfo, pagingOptions);
            queryable = ApplyFilter(queryable, pagingInfo, pagingOptions);
            queryable = ApplySorting(queryable, pagingInfo, pagingOptions);

            return queryable;
        }

        private static IQueryable<TEntity> ApplySearch<TEntity>(IQueryable<TEntity> queryable, PagingInfo pagingInfo, PagingOptions<TEntity> pagingOptions)
        {
            if (string.IsNullOrEmpty(pagingInfo.Search) || pagingOptions.SearchPredicateFactory == null)
            {
                return queryable;
            }

            var searchPredicate = pagingOptions.SearchPredicateFactory(pagingInfo.Search!);
            Trace.WriteLine($"Paging.Search \"{pagingInfo.Search}\"");
            return queryable.Where(searchPredicate);
        }

        private static IQueryable<TEntity> ApplyFilter<TEntity>(IQueryable<TEntity> queryable, PagingInfo pagingInfo, PagingOptions<TEntity> pagingOptions)
        {
            var predicate = FilterPredicateBuilder.Build(pagingInfo.Filter, pagingOptions);
            if (predicate != null)
            {
                queryable = queryable.Where(predicate);
            }

            return queryable;
        }

        private static IQueryable<TEntity> ApplySorting<TEntity>(IQueryable<TEntity> queryable, PagingInfo pagingInfo, PagingOptions<TEntity> pagingOptions)
        {
            var appliedSortCount = 0;

            foreach (var sorting in pagingInfo.Sorting)
            {
                var externalName = sorting.Key;
                var sortOrder = pagingInfo.Reverse ? Invert(sorting.Value) : sorting.Value;

                if (sortOrder == SortOrder.None)
                {
                    // SortOrder.None means "no sort" for this property; skip it entirely.
                    continue;
                }

                if (!pagingOptions.TryGetSortDefinition(externalName, out var sortDefinition))
                {
                    switch (pagingOptions.UnknownSortPropertyHandling)
                    {
                        case UnknownPropertyHandling.Ignore:
                            Trace.WriteLine($"Paging.SortBy: Sort property '{externalName}' is ignored (not registered).");
                            continue;

                        case UnknownPropertyHandling.Throw:
                        default:
                            throw new PagingException($"Sort property '{externalName}' is not allowed.", externalName);
                    }
                }

                var keySelector = sortDefinition.GetSortKeySelector();

                Trace.WriteLine($"Paging.SortBy \"{externalName} {sortOrder}\"{(pagingInfo.Reverse ? " (Reversed)" : "")}");
                queryable = SortExpressionBuilder.ApplyOrderBy(queryable, keySelector, sortOrder, isFirst: appliedSortCount == 0);
                appliedSortCount++;
            }

            return ApplyDefaultSort(queryable, pagingOptions, appliedSortCount);
        }

        private static IQueryable<TEntity> ApplyDefaultSort<TEntity>(IQueryable<TEntity> queryable, PagingOptions<TEntity> pagingOptions, int appliedSortCount)
        {
            // The default sort order is the primary order when no sorting is requested
            // and is appended as tie-breaker otherwise. It is never affected by PagingInfo.Reverse.
            foreach (var defaultSort in pagingOptions.DefaultSortDefinitions)
            {
                if (defaultSort.SortOrder == SortOrder.None)
                {
                    continue;
                }

                Trace.WriteLine($"Paging.OrderByDefault ({defaultSort.SortOrder})");
                queryable = SortExpressionBuilder.ApplyOrderBy(queryable, defaultSort.GetKeySelector(), defaultSort.SortOrder, isFirst: appliedSortCount == 0);
                appliedSortCount++;
            }

            return queryable;
        }

        private static SortOrder Invert(SortOrder sortOrder)
        {
            return sortOrder switch
            {
                SortOrder.Asc => SortOrder.Desc,
                SortOrder.Desc => SortOrder.Asc,
                _ => SortOrder.None,
            };
        }
    }
}
