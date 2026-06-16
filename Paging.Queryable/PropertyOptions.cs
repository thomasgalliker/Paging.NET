using System.Linq.Expressions;
using Paging.Queryable.Internals;

namespace Paging.Queryable
{
    /// <summary>
    /// Fluent configuration chain for a property registered with
    /// <see cref="PagingOptions{TEntity}.Property{TKey}(Expression{Func{TEntity, TKey}})"/>.
    /// Every property must declare its capabilities explicitly
    /// via <see cref="Sortable()"/> and/or <see cref="Filterable()"/>.
    /// </summary>
    public class PropertyOptions<TEntity>
    {
        private readonly PagingOptions<TEntity> owner;
        private readonly PropertyDefinition<TEntity> propertyDefinition;

        internal PropertyOptions(PagingOptions<TEntity> owner, PropertyDefinition<TEntity> propertyDefinition)
        {
            this.owner = owner;
            this.propertyDefinition = propertyDefinition;
        }

        /// <summary>
        /// Maps the property to an external name, i.e. the name used
        /// in <see cref="PagingInfo.SortBy"/> and <see cref="PagingInfo.Filter"/>.
        /// </summary>
        public PropertyOptions<TEntity> HasName(string externalName)
        {
            this.owner.RenameDefinition(this.propertyDefinition, externalName);
            return this;
        }

        /// <summary>
        /// Declares the property as sortable using its property path.
        /// </summary>
        public PropertyOptions<TEntity> Sortable()
        {
            this.owner.ThrowIfFrozen();
            this.propertyDefinition.DeclareSortable();
            return this;
        }

        /// <summary>
        /// Declares the property as sortable using a custom sort key expression,
        /// e.g. a computed value which does not exist as entity property.
        /// The expression must be translatable by the query provider.
        /// </summary>
        public PropertyOptions<TEntity> Sortable<TKey>(Expression<Func<TEntity, TKey>> keySelector)
        {
            this.owner.ThrowIfFrozen();

            if (keySelector == null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }

            this.propertyDefinition.DeclareSortable((LambdaExpression)keySelector);
            return this;
        }

        /// <summary>
        /// Declares the property as sortable using a custom sort key expression factory.
        /// The factory is evaluated on every query, which is useful for time-dependent
        /// sort keys (e.g. expressions capturing the current date/time).
        /// </summary>
        public PropertyOptions<TEntity> Sortable<TKey>(Func<Expression<Func<TEntity, TKey>>> keySelectorFactory)
        {
            this.owner.ThrowIfFrozen();

            if (keySelectorFactory == null)
            {
                throw new ArgumentNullException(nameof(keySelectorFactory));
            }

            this.propertyDefinition.DeclareSortable(() => keySelectorFactory());
            return this;
        }

        /// <summary>
        /// Declares the property as filterable using its property path
        /// and the built-in filter value semantics.
        /// </summary>
        public PropertyOptions<TEntity> Filterable()
        {
            this.owner.ThrowIfFrozen();
            this.propertyDefinition.DeclareFilterable();
            return this;
        }

        /// <summary>
        /// Declares the property as filterable using a custom filter predicate factory.
        /// The factory receives the filter value from a <see cref="FilterCondition"/>
        /// and returns a predicate expression, or <c>null</c> to skip the filter.
        /// </summary>
        public PropertyOptions<TEntity> Filterable(Func<object?, Expression<Func<TEntity, bool>>?> predicateFactory)
        {
            this.owner.ThrowIfFrozen();

            if (predicateFactory == null)
            {
                throw new ArgumentNullException(nameof(predicateFactory));
            }

            this.propertyDefinition.DeclareFilterable(predicateFactory);
            return this;
        }

        /// <summary>
        /// Declares the property as filterable using a custom filter predicate factory
        /// that also receives the requested <see cref="FilterOperator"/>.
        /// The factory returns a predicate expression, or <c>null</c> to skip the filter.
        /// </summary>
        public PropertyOptions<TEntity> Filterable(Func<FilterOperator, object?, Expression<Func<TEntity, bool>>?> predicateFactory)
        {
            this.owner.ThrowIfFrozen();

            if (predicateFactory == null)
            {
                throw new ArgumentNullException(nameof(predicateFactory));
            }

            this.propertyDefinition.DeclareFilterable(predicateFactory);
            return this;
        }
    }
}
