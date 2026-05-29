using System.Diagnostics;
using System.Linq.Expressions;

namespace Paging.Queryable
{
    public static class PagingInfoExtensions
    {
        /// <summary>
        /// Creates a <see cref="PaginationSet{TEntity}"/> from a queryable source without mapping the entity type.
        /// </summary>
        /// <typeparam name="TEntity">The entity type returned by the query.</typeparam>
        /// <param name="pagingInfo">The paging information to apply. If <c>null</c>, all items are returned.</param>
        /// <param name="queryable">The source query.</param>
        /// <param name="searchPredicate">An optional predicate used for free-text search.</param>
        /// <returns>A pagination set containing the queried entities.</returns>
        public static PaginationSet<TEntity> CreatePaginationSet<TEntity>(this PagingInfo? pagingInfo, IQueryable<TEntity> queryable, Expression<Func<TEntity, bool>>? searchPredicate = null)
        {
            return pagingInfo.CreatePaginationSet(queryable, entities => entities, searchPredicate);
        }

        /// <summary>
        /// Creates a <see cref="PaginationSet{TDto}"/> from a queryable source and maps the queried entities to another type.
        /// </summary>
        /// <typeparam name="TEntity">The entity type returned by the query.</typeparam>
        /// <typeparam name="TDto">The result type stored in the pagination set.</typeparam>
        /// <param name="pagingInfo">The paging information to apply. If <c>null</c>, all items are returned.</param>
        /// <param name="queryable">The source query.</param>
        /// <param name="mapEntitiesToDtos">Maps the queried entities to the target result type.</param>
        /// <param name="searchPredicate">An optional predicate used for free-text search.</param>
        /// <returns>A pagination set containing the mapped items.</returns>
        public static PaginationSet<TDto> CreatePaginationSet<TEntity, TDto>(this PagingInfo? pagingInfo, IQueryable<TEntity> queryable, Func<IEnumerable<TEntity>, IEnumerable<TDto>> mapEntitiesToDtos, Expression<Func<TEntity, bool>>? searchPredicate = null)
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

                // Apply filtering
                if (pagingInfo.Filter.Any())
                {
                    queryable = queryable.ApplyFilter(pagingInfo.Filter);
                }

                var totalCount = queryable.Count();

                // Apply sorting
                if (!string.IsNullOrEmpty(pagingInfo.SortBy))
                {
                    queryable = queryable.OrderBy(pagingInfo.Sorting, pagingInfo.Reverse);
                }
                else
                {
                    queryable = queryable.OrderByDefault();
                }

                // Apply paging
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
                    entities = Enumerable.Empty<TEntity>();
                }
                else
                {
                    entities = queryable.ToList();
                }

                var dtos = mapEntitiesToDtos(entities);
                var paginationSet = new PaginationSet<TDto>(pagingInfo, dtos, totalCount, totalCountUnfiltered);
                return paginationSet;
            }
        }
    }
}
