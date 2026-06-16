using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Paging.Queryable;
using Paging.Queryable.Internals;

namespace Paging.EF
{
    /// <summary>
    ///     Asynchronous, Entity Framework Core-backed terminals for Paging.Queryable.
    ///     These materialize queries with EF Core's <c>CountAsync</c>/<c>ToListAsync</c> and honor cancellation.
    /// </summary>
    public static class PaginationQueryableExtensions
    {
        /// <summary>
        ///     Counts and pages an already-shaped (filtered, sorted and optionally projected) query asynchronously.
        ///     Shape the query first with <see cref="Paging.Queryable.PaginationQueryableExtensions.ApplyPaging{TEntity}" />.
        /// </summary>
        public static async Task<PaginationSet<T>> ToPaginationSetAsync<T>(this IQueryable<T> queryable, PagingInfo? pagingInfo, CancellationToken cancellationToken = default)
        {
            if (queryable == null)
            {
                throw new ArgumentNullException(nameof(queryable));
            }

            if (pagingInfo == null)
            {
                return new PaginationSet<T>(await queryable.ToListAsync(cancellationToken).ConfigureAwait(false));
            }

            var totalCount = await queryable.CountAsync(cancellationToken).ConfigureAwait(false);
            var items = await MaterializeAsync(queryable, pagingInfo, cancellationToken).ConfigureAwait(false);

            return new PaginationSet<T>(pagingInfo, items, totalCount, totalCount);
        }

        /// <summary>
        ///     Shapes the query with <paramref name="pagingOptions" /> and returns the paged
        ///     <see cref="PaginationSet{TEntity}" /> asynchronously.
        /// </summary>
        public static async Task<PaginationSet<TEntity>> ToPaginationSetAsync<TEntity>(this IQueryable<TEntity> queryable, PagingInfo? pagingInfo, PagingOptions<TEntity> pagingOptions,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(queryable);
            ArgumentNullException.ThrowIfNull(pagingOptions);

            pagingOptions.Freeze();

            var totalCountUnfiltered = await ResolveUnfilteredCountAsync(queryable, pagingInfo, pagingOptions, cancellationToken).ConfigureAwait(false);
            var shaped = queryable.ApplyPaging(pagingInfo, pagingOptions);
            var paginationSet = await shaped.ToPaginationSetAsync(pagingInfo, cancellationToken).ConfigureAwait(false);

            if (totalCountUnfiltered.HasValue)
            {
                paginationSet.TotalCountUnfiltered = totalCountUnfiltered.Value;
            }

            return paginationSet;
        }

        /// <summary>
        ///     Shapes the query with <paramref name="pagingOptions" />, projects each entity to
        ///     <typeparamref name="TDto" /> via the configured map expression (translated to SQL)
        ///     and returns the paged <see cref="PaginationSet{TDto}" /> asynchronously.
        /// </summary>
        public static async Task<PaginationSet<TDto>> ToPaginationSetAsync<TEntity, TDto>(this IQueryable<TEntity> queryable, PagingInfo? pagingInfo, PagingOptions<TEntity, TDto> pagingOptions,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(queryable);
            ArgumentNullException.ThrowIfNull(pagingOptions);

            pagingOptions.Freeze();

            var totalCountUnfiltered = await ResolveUnfilteredCountAsync(queryable, pagingInfo, pagingOptions, cancellationToken).ConfigureAwait(false);
            var shaped = queryable.ApplyPaging(pagingInfo, pagingOptions);
            var projected = shaped.Select(pagingOptions.MapExpression);
            var paginationSet = await projected.ToPaginationSetAsync(pagingInfo, cancellationToken).ConfigureAwait(false);

            if (totalCountUnfiltered.HasValue)
            {
                paginationSet.TotalCountUnfiltered = totalCountUnfiltered.Value;
            }

            return paginationSet;
        }

        private static async Task<int?> ResolveUnfilteredCountAsync<TEntity>(IQueryable<TEntity> queryable, PagingInfo? pagingInfo, PagingOptions<TEntity> pagingOptions, CancellationToken cancellationToken)
        {
            if (pagingInfo == null || !pagingOptions.UnfilteredCountIncluded)
            {
                return null;
            }

            return await queryable.CountAsync(cancellationToken).ConfigureAwait(false);
        }

        private static async Task<IEnumerable<T>> MaterializeAsync<T>(IQueryable<T> queryable, PagingInfo pagingInfo, CancellationToken cancellationToken)
        {
            switch (PagingMath.GetPageMode(pagingInfo))
            {
                case PageMode.Page:
                    var skip = PagingMath.GetSkip(pagingInfo);
                    var take = PagingMath.GetTake(pagingInfo);
                    Trace.WriteLine($"Paging.Skip({skip}).Take({take})");
                    return await queryable.Skip(skip).Take(take).ToListAsync(cancellationToken).ConfigureAwait(false);

                case PageMode.TotalsOnly:
                    return Enumerable.Empty<T>();

                case PageMode.All:
                default:
                    return await queryable.ToListAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}