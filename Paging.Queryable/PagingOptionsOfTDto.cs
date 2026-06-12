namespace Paging.Queryable
{
    /// <summary>
    /// Configures how a <see cref="PagingInfo"/> request is applied to a query of <typeparamref name="TEntity"/>
    /// and how the resulting entities are mapped to <typeparamref name="TDto"/>.
    /// See <see cref="PagingOptions{TEntity}"/> for the configuration surface.
    /// </summary>
    public class PagingOptions<TEntity, TDto> : PagingOptions<TEntity>
    {
        private Func<IEnumerable<TEntity>, IEnumerable<TDto>>? mapEntitiesToDtos;

        public PagingOptions()
        {
        }

        public PagingOptions(Action<PagingOptions<TEntity, TDto>> configure)
        {
            configure?.Invoke(this);
        }

        /// <summary>
        /// Configures the mapping from the queried entities to the result type.
        /// This configuration is mandatory.
        /// </summary>
        public void Map(Func<IEnumerable<TEntity>, IEnumerable<TDto>> mapEntitiesToDtos)
        {
            this.ThrowIfFrozen();
            this.mapEntitiesToDtos = mapEntitiesToDtos ?? throw new ArgumentNullException(nameof(mapEntitiesToDtos));
        }

        internal Func<IEnumerable<TEntity>, IEnumerable<TDto>> MapEntities => this.mapEntitiesToDtos!;

        protected override void Validate()
        {
            base.Validate();

            if (this.mapEntitiesToDtos == null)
            {
                throw new InvalidOperationException(
                    $"Map(...) must be configured on PagingOptions<{typeof(TEntity).Name}, {typeof(TDto).Name}>.");
            }
        }
    }
}
