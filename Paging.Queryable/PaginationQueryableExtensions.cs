using System.Diagnostics;
using Paging.Queryable.Internals;

namespace Paging.Queryable
{
    public static class PaginationQueryableExtensions
    {
        /// <summary>
        /// Applies the search, filter and sort defined by <paramref name="pagingInfo"/> and
        /// <paramref name="pagingOptions"/> to the query and returns the shaped <see cref="IQueryable{TEntity}"/>.
        /// </summary>
        /// <remarks>
        /// This is the composition seam: it does <b>not</b> apply <c>Skip</c>/<c>Take</c> and does not
        /// materialize the query. Compose further (e.g. project with <c>Select(...)</c>) and then call
        /// <see cref="ToPaginationSet{T}(IQueryable{T}, PagingInfo?)"/> (or the EF Core async equivalent)
        /// to count and page. Only properties declared in <paramref name="pagingOptions"/> can be
        /// sorted or filtered.
        /// </remarks>
        /// <param name="queryable">The source query.</param>
        /// <param name="pagingInfo">The paging request. If <c>null</c>, only the default sort order is applied.</param>
        /// <param name="pagingOptions">The options controlling sorting, filtering and search.</param>
        public static IQueryable<TEntity> ApplyPaging<TEntity>(this IQueryable<TEntity> queryable, PagingInfo? pagingInfo, PagingOptions<TEntity> pagingOptions)
        {
            if (queryable == null)
            {
                throw new ArgumentNullException(nameof(queryable));
            }

            if (pagingOptions == null)
            {
                throw new ArgumentNullException(nameof(pagingOptions));
            }

            pagingOptions.Freeze();
            return QueryShaper.Shape(queryable, pagingInfo, pagingOptions);
        }

        /// <summary>
        /// Counts and pages an already-shaped (filtered, sorted and optionally projected) query,
        /// returning a <see cref="PaginationSet{T}"/>.
        /// </summary>
        /// <remarks>
        /// This is the materialization seam. It does not apply any filtering or sorting; shape the
        /// query first with <see cref="ApplyPaging{TEntity}"/>. <see cref="PaginationSet{T}.TotalCountUnfiltered"/>
        /// is <c>null</c> here, since the unfiltered source is not known.
        /// </remarks>
        /// <param name="queryable">The shaped source query.</param>
        /// <param name="pagingInfo">The paging request. If <c>null</c>, all items are returned in a single page.</param>
        public static PaginationSet<T> ToPaginationSet<T>(this IQueryable<T> queryable, PagingInfo? pagingInfo)
        {
            if (queryable == null)
            {
                throw new ArgumentNullException(nameof(queryable));
            }

            if (pagingInfo == null)
            {
                return new PaginationSet<T>(queryable.ToList());
            }

            var totalCount = queryable.Count();
            var items = Materialize(queryable, pagingInfo);

            return new PaginationSet<T>(pagingInfo, items, totalCount, totalCountUnfiltered: null);
        }

        /// <summary>
        /// Shapes the query with <paramref name="pagingOptions"/> and returns the paged
        /// <see cref="PaginationSet{TEntity}"/>. Convenience for <c>queryable.ApplyPaging(...).ToPaginationSet(...)</c>.
        /// </summary>
        /// <param name="queryable">The source query.</param>
        /// <param name="pagingInfo">The paging request. If <c>null</c>, all items are returned.</param>
        /// <param name="pagingOptions">The options controlling sorting, filtering and search.</param>
        public static PaginationSet<TEntity> ToPaginationSet<TEntity>(this IQueryable<TEntity> queryable, PagingInfo? pagingInfo, PagingOptions<TEntity> pagingOptions)
        {
            if (queryable == null)
            {
                throw new ArgumentNullException(nameof(queryable));
            }

            if (pagingOptions == null)
            {
                throw new ArgumentNullException(nameof(pagingOptions));
            }

            pagingOptions.Freeze();

            var totalCountUnfiltered = ResolveUnfilteredCount(queryable, pagingInfo, pagingOptions);
            var shaped = QueryShaper.Shape(queryable, pagingInfo, pagingOptions);
            var paginationSet = shaped.ToPaginationSet(pagingInfo);
            paginationSet.TotalCountUnfiltered = totalCountUnfiltered;

            return paginationSet;
        }

        /// <summary>
        /// Shapes the query with <paramref name="pagingOptions"/>, projects each entity to
        /// <typeparamref name="TDto"/> via the configured <see cref="PagingOptions{TEntity, TDto}.Map"/>
        /// expression (translated to SQL) and returns the paged <see cref="PaginationSet{TDto}"/>.
        /// </summary>
        /// <param name="queryable">The source query.</param>
        /// <param name="pagingInfo">The paging request. If <c>null</c>, all items are returned.</param>
        /// <param name="pagingOptions">The options controlling sorting, filtering, search and projection.</param>
        public static PaginationSet<TDto> ToPaginationSet<TEntity, TDto>(this IQueryable<TEntity> queryable, PagingInfo? pagingInfo, PagingOptions<TEntity, TDto> pagingOptions)
        {
            if (queryable == null)
            {
                throw new ArgumentNullException(nameof(queryable));
            }

            if (pagingOptions == null)
            {
                throw new ArgumentNullException(nameof(pagingOptions));
            }

            pagingOptions.Freeze();

            var totalCountUnfiltered = ResolveUnfilteredCount(queryable, pagingInfo, pagingOptions);
            var shaped = QueryShaper.Shape(queryable, pagingInfo, pagingOptions);
            var projected = shaped.Select(pagingOptions.MapExpression);
            var paginationSet = projected.ToPaginationSet(pagingInfo);
            paginationSet.TotalCountUnfiltered = totalCountUnfiltered;

            return paginationSet;
        }

        private static int? ResolveUnfilteredCount<TEntity>(IQueryable<TEntity> queryable, PagingInfo? pagingInfo, PagingOptions<TEntity> pagingOptions)
        {
            if (pagingInfo == null || !pagingOptions.UnfilteredCountIncluded)
            {
                return null;
            }

            return queryable.Count();
        }

        private static IEnumerable<T> Materialize<T>(IQueryable<T> queryable, PagingInfo pagingInfo)
        {
            switch (PagingMath.GetPageMode(pagingInfo))
            {
                case PageMode.Page:
                    var skip = PagingMath.GetSkip(pagingInfo);
                    var take = PagingMath.GetTake(pagingInfo);
                    Trace.WriteLine($"Paging.Skip({skip}).Take({take})");
                    return queryable.Skip(skip).Take(take).ToList();

                case PageMode.TotalsOnly:
                    return Enumerable.Empty<T>();

                case PageMode.All:
                default:
                    return queryable.ToList();
            }
        }
    }
}
