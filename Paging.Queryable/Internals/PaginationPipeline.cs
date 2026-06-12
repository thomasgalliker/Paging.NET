using System.Diagnostics;
using System.Linq.Expressions;

namespace Paging.Queryable.Internals
{
    /// <summary>
    /// Applies a <see cref="PagingInfo"/> request to a query in the following order:
    /// total count (unfiltered) → search → filter → total count → sort → page → map.
    /// </summary>
    internal static class PaginationPipeline
    {
        internal static PaginationSet<TResult> Execute<TEntity, TResult>(IQueryable<TEntity> queryable, PagingInfo? pagingInfo, PagingOptions<TEntity> pagingOptions, Func<IEnumerable<TEntity>, IEnumerable<TResult>> map)
        {
            pagingOptions.Freeze();

            if (pagingInfo == null)
            {
                var allItems = ApplyDefaultSort(queryable, pagingOptions, appliedSortCount: 0);
                var allDtos = map(allItems);
                return new PaginationSet<TResult>(allDtos);
            }

            var totalCountUnfiltered = queryable.Count();

            queryable = ApplySearch(queryable, pagingInfo, pagingOptions);
            queryable = ApplyFilter(queryable, pagingInfo, pagingOptions);

            var totalCount = queryable.Count();

            queryable = ApplySorting(queryable, pagingInfo, pagingOptions);

            IEnumerable<TEntity> entities;
            if (pagingInfo.ItemsPerPage > 0)
            {
                var skip = (pagingInfo.CurrentPage - pagingInfo.FirstPageIndex) * pagingInfo.ItemsPerPage.Value;
                var take = pagingInfo.ItemsPerPage.Value;
                Trace.WriteLine($"Paging.Skip({skip}).Take({take})");
                entities = queryable.Skip(skip).Take(take).ToList();
            }
            else if (pagingInfo.ItemsPerPage == 0)
            {
                // ItemsPerPage = 0 returns totals only and no items
                entities = Enumerable.Empty<TEntity>();
            }
            else
            {
                // ItemsPerPage = null returns all matching items
                entities = queryable.ToList();
            }

            var dtos = map(entities);
            return new PaginationSet<TResult>(pagingInfo, dtos, totalCount, totalCountUnfiltered);
        }

        private static IQueryable<TEntity> ApplySearch<TEntity>(
            IQueryable<TEntity> queryable,
            PagingInfo pagingInfo,
            PagingOptions<TEntity> pagingOptions)
        {
            if (string.IsNullOrEmpty(pagingInfo.Search) || pagingOptions.SearchPredicateFactory == null)
            {
                return queryable;
            }

            var searchPredicate = pagingOptions.SearchPredicateFactory(pagingInfo.Search!);
            Trace.WriteLine($"Paging.Search \"{pagingInfo.Search}\"");
            return queryable.Where(searchPredicate);
        }

        private static IQueryable<TEntity> ApplyFilter<TEntity>(
            IQueryable<TEntity> queryable,
            PagingInfo pagingInfo,
            PagingOptions<TEntity> pagingOptions)
        {
            foreach (var filter in pagingInfo.Filter)
            {
                if (filter.Value == null)
                {
                    // Ignore null values
                    continue;
                }

                if (pagingOptions.TryGetFilterDefinition(filter.Key, out var filterDefinition))
                {
                    if (filterDefinition.FilterPredicateFactory != null)
                    {
                        var predicate = filterDefinition.FilterPredicateFactory(filter.Value);
                        if (predicate != null)
                        {
                            Trace.WriteLine($"Paging.ApplyFilter: Custom filter predicate for property '{filter.Key}'");
                            queryable = queryable.Where(predicate);
                        }
                    }
                    else
                    {
                        queryable = FilterExpressionBuilder.ApplyFilter(queryable, filterDefinition.PropertyPath, filter.Value);
                    }

                    continue;
                }

                switch (pagingOptions.UnknownFilterPropertyHandling)
                {
                    case UnknownPropertyHandling.Throw:
                        throw new PagingException($"Filter property '{filter.Key}' is not allowed.", filter.Key);

                    case UnknownPropertyHandling.Ignore:
                        Trace.WriteLine($"Paging.ApplyFilter: Filter property '{filter.Key}' is ignored (not registered).");
                        continue;

                    case UnknownPropertyHandling.Allow:
                    default:
                        queryable = FilterExpressionBuilder.ApplyFilter(queryable, filter.Key, filter.Value);
                        continue;
                }
            }

            return queryable;
        }

        private static IQueryable<TEntity> ApplySorting<TEntity>(
            IQueryable<TEntity> queryable,
            PagingInfo pagingInfo,
            PagingOptions<TEntity> pagingOptions)
        {
            var appliedSortCount = 0;

            foreach (var sorting in pagingInfo.Sorting)
            {
                var externalName = sorting.Key;
                var sortOrder = pagingInfo.Reverse ? Invert(sorting.Value) : sorting.Value;

                LambdaExpression? keySelector;

                if (pagingOptions.TryGetSortDefinition(externalName, out var sortDefinition))
                {
                    keySelector = sortDefinition.GetSortKeySelector();
                }
                else
                {
                    switch (pagingOptions.UnknownSortPropertyHandling)
                    {
                        case UnknownPropertyHandling.Throw:
                            throw new PagingException($"Sort property '{externalName}' is not allowed.", externalName);

                        case UnknownPropertyHandling.Ignore:
                            Trace.WriteLine($"Paging.SortBy: Sort property '{externalName}' is ignored (not registered).");
                            continue;

                        case UnknownPropertyHandling.Allow:
                        default:
                            keySelector = SortExpressionBuilder.CreatePropertyPathLambda<TEntity>(externalName);
                            if (keySelector == null)
                            {
                                Trace.WriteLine($"Paging.SortBy: Sort property '{externalName}' cannot be resolved on type '{typeof(TEntity).Name}'.");
                                continue;
                            }

                            break;
                    }
                }

                Trace.WriteLine($"Paging.SortBy \"{externalName} {sortOrder}\"{(pagingInfo.Reverse ? " (Reversed)" : "")}");
                queryable = SortExpressionBuilder.ApplyOrderBy(queryable, keySelector, sortOrder, isFirst: appliedSortCount == 0);
                appliedSortCount++;
            }

            return ApplyDefaultSort(queryable, pagingOptions, appliedSortCount);
        }

        private static IQueryable<TEntity> ApplyDefaultSort<TEntity>(
            IQueryable<TEntity> queryable,
            PagingOptions<TEntity> pagingOptions,
            int appliedSortCount)
        {
            // The default sort order is the primary order when no sorting is requested
            // and is appended as tie-breaker otherwise. It is never affected by PagingInfo.Reverse.
            foreach (var defaultSort in pagingOptions.DefaultSortDefinitions)
            {
                Trace.WriteLine($"Paging.OrderByDefault ({defaultSort.SortOrder})");
                queryable = SortExpressionBuilder.ApplyOrderBy(queryable, defaultSort.GetKeySelector(), defaultSort.SortOrder, isFirst: appliedSortCount == 0);
                appliedSortCount++;
            }

            return queryable;
        }

        private static SortOrder Invert(SortOrder sortOrder)
        {
            return sortOrder == SortOrder.Asc ? SortOrder.Desc : SortOrder.Asc;
        }
    }
}
