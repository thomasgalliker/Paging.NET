using Paging.Queryable.Internals;

namespace Paging.Queryable
{
    public static class PaginationQueryableExtensions
    {
        /// <summary>
        /// Creates a <see cref="PaginationSet{TEntity}"/> from a queryable source
        /// using permissive default options: all entity properties are sortable and filterable
        /// by their (dotted) property path, unresolvable names are silently skipped,
        /// free-text search is not supported and no default sort order is applied.
        /// Use a <see cref="PagingOptions{TEntity}"/> overload to restrict, map or customize
        /// sorting and filtering.
        /// </summary>
        /// <param name="queryable">The source query.</param>
        /// <param name="pagingInfo">The paging information to apply. If <c>null</c>, all items are returned.</param>
        public static PaginationSet<TEntity> ToPaginationSet<TEntity>(
            this IQueryable<TEntity> queryable,
            PagingInfo? pagingInfo)
        {
            if (queryable == null)
            {
                throw new ArgumentNullException(nameof(queryable));
            }

            return PaginationPipeline.Execute(queryable, pagingInfo, PermissivePagingOptions<TEntity>.Instance, entities => entities);
        }

        /// <summary>
        /// Creates a <see cref="PaginationSet{TEntity}"/> from a queryable source
        /// applying the given <paramref name="pagingOptions"/>.
        /// </summary>
        /// <param name="queryable">The source query.</param>
        /// <param name="pagingInfo">The paging information to apply. If <c>null</c>, all items are returned.</param>
        /// <param name="pagingOptions">The options controlling sorting, filtering and search.</param>
        public static PaginationSet<TEntity> ToPaginationSet<TEntity>(
            this IQueryable<TEntity> queryable,
            PagingInfo? pagingInfo,
            PagingOptions<TEntity> pagingOptions)
        {
            if (queryable == null)
            {
                throw new ArgumentNullException(nameof(queryable));
            }

            if (pagingOptions == null)
            {
                throw new ArgumentNullException(nameof(pagingOptions));
            }

            return PaginationPipeline.Execute(queryable, pagingInfo, pagingOptions, entities => entities);
        }

        /// <summary>
        /// Creates a <see cref="PaginationSet{TDto}"/> from a queryable source
        /// applying the given <paramref name="pagingOptions"/> and mapping the queried entities
        /// to <typeparamref name="TDto"/> using the configured
        /// <see cref="PagingOptions{TEntity, TDto}.Map"/> delegate.
        /// </summary>
        /// <param name="queryable">The source query.</param>
        /// <param name="pagingInfo">The paging information to apply. If <c>null</c>, all items are returned.</param>
        /// <param name="pagingOptions">The options controlling sorting, filtering, search and mapping.</param>
        public static PaginationSet<TDto> ToPaginationSet<TEntity, TDto>(
            this IQueryable<TEntity> queryable,
            PagingInfo? pagingInfo,
            PagingOptions<TEntity, TDto> pagingOptions)
        {
            if (queryable == null)
            {
                throw new ArgumentNullException(nameof(queryable));
            }

            if (pagingOptions == null)
            {
                throw new ArgumentNullException(nameof(pagingOptions));
            }

            return PaginationPipeline.Execute(queryable, pagingInfo, pagingOptions, entities => pagingOptions.MapEntities(entities));
        }

        private static class PermissivePagingOptions<TEntity>
        {
            internal static readonly PagingOptions<TEntity> Instance = CreateInstance();

            private static PagingOptions<TEntity> CreateInstance()
            {
                var pagingOptions = new PagingOptions<TEntity>();
                pagingOptions.UnknownProperties(UnknownPropertyHandling.Allow);
                pagingOptions.Freeze();
                return pagingOptions;
            }
        }
    }
}
