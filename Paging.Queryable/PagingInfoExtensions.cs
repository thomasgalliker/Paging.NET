using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
#if NETSTANDARD1_3
using System.Linq.Dynamic.Core;
#else
using System.Linq.Dynamic;
#endif

namespace Paging.Queryable
{
    public static class PagingInfoExtensions
    {
        public static PaginationSet<TEntity> CreatePaginationSet<TEntity>(this PagingInfo pagingInfo, IQueryable<TEntity> queryable, Expression<Func<TEntity, bool>> filterPredicate = null)
        {
            return pagingInfo.CreatePaginationSet(queryable, entities => entities);
        }

        public static PaginationSet<TDto> CreatePaginationSet<TEntity, TDto>(this PagingInfo pagingInfo, IQueryable<TEntity> queryable, Func<IEnumerable<TEntity>, IEnumerable<TDto>> mapEntitiesToDtos, Expression<Func<TEntity, bool>> filterPredicate = null)
        {
            if (pagingInfo == null)
            {
                var dtos = mapEntitiesToDtos(queryable);
                var paginationSet = new PaginationSet<TDto>(dtos);
                return paginationSet;
            }
            else
            {
                var totalCountUnfiltered = queryable.Count();

                // Filter
                if (filterPredicate != null && pagingInfo.Search != null)
                {
                    queryable = queryable.Where(filterPredicate);
                }

                var totalCount = queryable.Count();

                // Order
                if (!string.IsNullOrEmpty(pagingInfo.SortBy))
                {
                    var sortBy = pagingInfo.SortBy.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (sortBy.Length == 1)
                    {
                        queryable = queryable.OrderBy(sortBy[0] + (pagingInfo.Reverse ? " descending" : ""));
                    }
                    if (sortBy.Length > 1)
                    {
                        queryable = queryable.OrderBy(pagingInfo.SortBy);
                    }
                }
                else
                {
                    // Need to OrderBy before Skip/Take. Maybe we should throw an exception here?
                    queryable = queryable.OrderBy("Id");
                }

                // Page
                if (pagingInfo.ItemsPerPage > 0)
                {
                    queryable = queryable.Skip((pagingInfo.CurrentPage - 1) * pagingInfo.ItemsPerPage).Take(pagingInfo.ItemsPerPage);
                }

                var dtos = mapEntitiesToDtos(queryable);
                var paginationSet = new PaginationSet<TDto>(pagingInfo, dtos, totalCount, totalCountUnfiltered);
                return paginationSet;
            }
        }

        public static PaginationSet<TTarget> Map<TSource, TTarget>(this PagingInfo pagingInfo, PaginationSet<TSource> paginationSet, Func<IEnumerable<TSource>, IEnumerable<TTarget>> mapSourceToTarget)
        {
            var targetItems = mapSourceToTarget(paginationSet.Items);
            var paginationSetTarget = new PaginationSet<TTarget>(pagingInfo, targetItems, paginationSet.TotalCount, paginationSet.TotalCountUnfiltered);
            return paginationSetTarget;
        }
    }
}
