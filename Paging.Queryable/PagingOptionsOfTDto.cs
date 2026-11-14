using System.Linq.Expressions;

namespace Paging.Queryable
{
    /// <summary>
    /// Configures how a <see cref="PagingInfo"/> request is applied to a query of <typeparamref name="TEntity"/>
    /// and how the resulting entities are projected to <typeparamref name="TDto"/>.
    /// See <see cref="PagingOptions{TEntity}"/> for the configuration surface.
    /// </summary>
    /// <remarks>
    /// The projection is configured as an <see cref="Expression{TDelegate}"/> so it is applied
    /// via <c>IQueryable.Select(...)</c> and translated to SQL by the query provider, fetching
    /// only the projected columns instead of materializing full entities.
    /// </remarks>
    public class PagingOptions<TEntity, TDto> : PagingOptions<TEntity>
    {
        private Expression<Func<TEntity, TDto>>? mapEntityToDto;

        public PagingOptions()
        {
        }

        public PagingOptions(Action<PagingOptions<TEntity, TDto>> configure)
        {
            configure?.Invoke(this);
        }

        /// <summary>
        /// Configures the projection from the queried entity to the result type.
        /// This configuration is mandatory. The expression must be translatable by the query provider.
        /// </summary>
        public void Map(Expression<Func<TEntity, TDto>> mapEntityToDto)
        {
            this.ThrowIfFrozen();
            this.mapEntityToDto = mapEntityToDto ?? throw new ArgumentNullException(nameof(mapEntityToDto));
        }

        internal Expression<Func<TEntity, TDto>> MapExpression => this.mapEntityToDto!;

        protected override void Validate()
        {
            base.Validate();

            if (this.mapEntityToDto == null)
            {
                throw new InvalidOperationException(
                    $"Map(...) must be configured on PagingOptions<{typeof(TEntity).Name}, {typeof(TDto).Name}>.");
            }
        }
    }
}
