using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using System.Linq.Expressions;
#if NETSTANDARD1_3
using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.Exceptions;
#else
using System.Linq.Dynamic;
#endif

namespace Paging.Queryable
{
    public static class PagingInfoExtensions
    {
        public static PaginationSet<TEntity> CreatePaginationSet<TEntity>(this PagingInfo pagingInfo, IQueryable<TEntity> queryable, Expression<Func<TEntity, bool>> searchPredicate = null)
        {
            return pagingInfo.CreatePaginationSet(queryable, entities => entities, searchPredicate);
        }

        public static PaginationSet<TDto> CreatePaginationSet<TEntity, TDto>(this PagingInfo pagingInfo, IQueryable<TEntity> queryable, Func<IEnumerable<TEntity>, IEnumerable<TDto>> mapEntitiesToDtos, Expression<Func<TEntity, bool>> searchPredicate = null)
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

                // Free-text search using custom search predicate
                if (pagingInfo.Search != null && searchPredicate != null)
                {
                    queryable = queryable.Where(searchPredicate);
                }

                // Property-based filter
                if (pagingInfo.Filter != null)
                {
                    queryable = queryable.ApplyFilter(pagingInfo.Filter);
                }

                var totalCount = queryable.Count();

                // Order
                if (!string.IsNullOrEmpty(pagingInfo.SortBy))
                {
                    queryable = queryable.OrderBy(pagingInfo.Sorting, pagingInfo.Reverse);
                }
                else
                {
                    queryable = queryable.OrderByDefault();
                }

                // Page
                if (pagingInfo.ItemsPerPage > 0)
                {
                    var skip = (pagingInfo.CurrentPage - 1) * pagingInfo.ItemsPerPage;
                    var take = pagingInfo.ItemsPerPage;
                    Trace.WriteLine($"Paging.Skip({skip}).Take({take})");
                    queryable = queryable.Skip(skip).Take(take);
                }

                var dtos = mapEntitiesToDtos(queryable.ToList());
                var paginationSet = new PaginationSet<TDto>(pagingInfo, dtos, totalCount, totalCountUnfiltered);
                return paginationSet;
            }
        }
    }
}
