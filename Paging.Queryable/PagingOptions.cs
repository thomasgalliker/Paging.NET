using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Paging.Queryable.Internals;

namespace Paging.Queryable
{
    /// <summary>
    /// Configures how a <see cref="PagingInfo"/> request is applied to a query of <typeparamref name="TEntity"/>:
    /// which properties are allowed for sorting and filtering, how external property names map to
    /// entity properties, custom sort expressions, custom filter predicates, free-text search
    /// and the default sort order.
    /// </summary>
    /// <remarks>
    /// Properties are registered with <see cref="Property{TKey}(Expression{Func{TEntity, TKey}})"/>
    /// and declare their capabilities explicitly via <see cref="PropertyOptions{TEntity}.Sortable()"/>
    /// and/or <see cref="PropertyOptions{TEntity}.Filterable()"/>.
    /// Configure the options inline using the <see cref="PagingOptions{TEntity}(Action{PagingOptions{TEntity}})"/>
    /// constructor or by inheriting from this class and configuring in the constructor
    /// (e.g. for dependency-injected option classes).
    /// The instance is frozen on first use and can safely be reused across queries and threads afterwards.
    /// </remarks>
    public class PagingOptions<TEntity>
    {
        private readonly List<PropertyDefinition<TEntity>> propertyDefinitions = new List<PropertyDefinition<TEntity>>();
        private readonly List<DefaultSortDefinition> defaultSortDefinitions = new List<DefaultSortDefinition>();

        private StringComparer nameComparer = StringComparer.OrdinalIgnoreCase;
        private Func<string, Expression<Func<TEntity, bool>>>? searchPredicateFactory;
        private UnknownPropertyHandling unknownSortPropertyHandling = UnknownPropertyHandling.Throw;
        private UnknownPropertyHandling unknownFilterPropertyHandling = UnknownPropertyHandling.Throw;
        private InvalidValueHandling invalidFilterValueHandling = InvalidValueHandling.Skip;
        private bool unfilteredCountIncluded;

        private volatile bool isFrozen;
        private Dictionary<string, PropertyDefinition<TEntity>>? sortLookup;
        private Dictionary<string, PropertyDefinition<TEntity>>? filterLookup;

        public PagingOptions()
        {
        }

        public PagingOptions(Action<PagingOptions<TEntity>> configure)
        {
            configure?.Invoke(this);
        }

        /// <summary>
        /// Registers a property. The external name defaults to the property path,
        /// e.g. <c>"Name"</c> or <c>"Owner.Name"</c>; use <see cref="PropertyOptions{TEntity}.HasName"/>
        /// to map a different external name. Declare the capabilities of the property
        /// with <see cref="PropertyOptions{TEntity}.Sortable()"/> and/or <see cref="PropertyOptions{TEntity}.Filterable()"/>.
        /// </summary>
        /// <param name="property">A property access expression, e.g. <c>e =&gt; e.Owner.Name</c>.</param>
        public PropertyOptions<TEntity> Property<TKey>(Expression<Func<TEntity, TKey>> property)
        {
            this.ThrowIfFrozen();

            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            var propertyPath = SortExpressionBuilder.GetPropertyPath(property)
                ?? throw new ArgumentException(
                    $"Expression '{property}' must be a property access chain, e.g. e => e.Owner.Name. " +
                    $"Use Property(externalName).Sortable(keySelector) to register computed sort expressions.",
                    nameof(property));

            return this.AddPropertyDefinition(new PropertyDefinition<TEntity>(propertyPath, propertyPath, (LambdaExpression)property));
        }

        /// <summary>
        /// Registers a to-many navigation for collection filtering. A filter condition on the resulting
        /// property matches entities whose collection contains at least one element satisfying the condition,
        /// i.e. it is applied as <c>e =&gt; e.Collection.Any(x =&gt; &lt;condition&gt;)</c> (translated to SQL <c>EXISTS</c>).
        /// The external name defaults to <c>"&lt;collection&gt;.&lt;element&gt;"</c>; use
        /// <see cref="PropertyOptions{TEntity}.HasName"/> to map a different external name.
        /// Collection registrations support filtering only (not sorting).
        /// </summary>
        /// <param name="collectionSelector">A property access expression to a collection, e.g. <c>e =&gt; e.Children</c>.</param>
        /// <param name="elementSelector">A property access expression on the collection element, e.g. <c>c =&gt; c.Name</c>.</param>
        public PropertyOptions<TEntity> Property<TElement, TKey>(
            Expression<Func<TEntity, IEnumerable<TElement>>> collectionSelector,
            Expression<Func<TElement, TKey>> elementSelector)
        {
            this.ThrowIfFrozen();

            if (collectionSelector == null)
            {
                throw new ArgumentNullException(nameof(collectionSelector));
            }

            if (elementSelector == null)
            {
                throw new ArgumentNullException(nameof(elementSelector));
            }

            var collectionPath = SortExpressionBuilder.GetPropertyPath(collectionSelector)
                ?? throw new ArgumentException(
                    $"Expression '{collectionSelector}' must be a property access chain to a collection, e.g. e => e.Children.",
                    nameof(collectionSelector));

            var elementPath = SortExpressionBuilder.GetPropertyPath(elementSelector)
                ?? throw new ArgumentException(
                    $"Expression '{elementSelector}' must be a property access chain on the element, e.g. c => c.Name.",
                    nameof(elementSelector));

            var externalName = $"{collectionPath}.{elementPath}";

            return this.AddPropertyDefinition(new PropertyDefinition<TEntity>(
                externalName, externalName, (LambdaExpression)collectionSelector, (LambdaExpression)elementSelector));
        }

        /// <summary>
        /// Registers a property by its external name. By default, the name is also used
        /// as (dotted) property path on <typeparamref name="TEntity"/> and is validated on first use;
        /// capabilities registered with custom expressions (e.g.
        /// <see cref="PropertyOptions{TEntity}.Sortable{TKey}(Expression{Func{TEntity, TKey}})"/>)
        /// do not require a matching entity property.
        /// </summary>
        public PropertyOptions<TEntity> Property(string externalName)
        {
            this.ThrowIfFrozen();
            EnsureValidExternalName(externalName);

            return this.AddPropertyDefinition(new PropertyDefinition<TEntity>(externalName, externalName));
        }

        /// <summary>
        /// Configures the default sort order. It serves two purposes:
        /// it is the primary sort order when the request does not specify any sorting,
        /// and it is appended as tie-breaker after the requested sort order
        /// (which keeps paging stable). Multiple calls append additional sort keys.
        /// The default sort order is never affected by <see cref="PagingInfo.Reverse"/>.
        /// </summary>
        public void DefaultSort<TKey>(Expression<Func<TEntity, TKey>> keySelector, SortOrder sortOrder = SortOrder.Asc)
        {
            this.ThrowIfFrozen();

            if (keySelector == null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }

            this.defaultSortDefinitions.Add(new DefaultSortDefinition((LambdaExpression)keySelector, sortOrder));
        }

        /// <summary>
        /// Configures the default sort order with a key selector factory
        /// which is evaluated on every query.
        /// See <see cref="DefaultSort{TKey}(Expression{Func{TEntity, TKey}}, SortOrder)"/>.
        /// </summary>
        public void DefaultSort<TKey>(Func<Expression<Func<TEntity, TKey>>> keySelectorFactory, SortOrder sortOrder = SortOrder.Asc)
        {
            this.ThrowIfFrozen();

            if (keySelectorFactory == null)
            {
                throw new ArgumentNullException(nameof(keySelectorFactory));
            }

            this.defaultSortDefinitions.Add(new DefaultSortDefinition(() => keySelectorFactory(), sortOrder));
        }

        /// <summary>
        /// Configures the free-text search predicate factory.
        /// The factory receives <see cref="PagingInfo.Search"/> and is only invoked
        /// when the search text is not null or empty.
        /// </summary>
        public void Search(Func<string, Expression<Func<TEntity, bool>>> searchPredicateFactory)
        {
            this.ThrowIfFrozen();
            this.searchPredicateFactory = searchPredicateFactory ?? throw new ArgumentNullException(nameof(searchPredicateFactory));
        }

        /// <summary>
        /// Configures how unknown sort property names are handled.
        /// Default is <see cref="UnknownPropertyHandling.Throw"/>.
        /// </summary>
        public void UnknownSortProperties(UnknownPropertyHandling handling)
        {
            this.ThrowIfFrozen();
            this.unknownSortPropertyHandling = handling;
        }

        /// <summary>
        /// Configures how unknown filter property names are handled.
        /// Default is <see cref="UnknownPropertyHandling.Throw"/>.
        /// </summary>
        public void UnknownFilterProperties(UnknownPropertyHandling handling)
        {
            this.ThrowIfFrozen();
            this.unknownFilterPropertyHandling = handling;
        }

        /// <summary>
        /// Configures how unknown sort and filter property names are handled.
        /// Default is <see cref="UnknownPropertyHandling.Throw"/>.
        /// </summary>
        public void UnknownProperties(UnknownPropertyHandling handling)
        {
            this.UnknownSortProperties(handling);
            this.UnknownFilterProperties(handling);
        }

        /// <summary>
        /// Configures how filter values that cannot be applied to the target property are handled
        /// (e.g. failed type conversions). Default is <see cref="InvalidValueHandling.Skip"/>,
        /// which silently drops the condition; use <see cref="InvalidValueHandling.Throw"/> to
        /// surface invalid requests to the caller instead.
        /// </summary>
        public void InvalidFilterValues(InvalidValueHandling handling)
        {
            this.ThrowIfFrozen();
            this.invalidFilterValueHandling = handling;
        }

        /// <summary>
        /// Configures the string comparer used to match external property names.
        /// Default is <see cref="StringComparer.OrdinalIgnoreCase"/>.
        /// </summary>
        public void NameComparer(StringComparer comparer)
        {
            this.ThrowIfFrozen();
            this.nameComparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        }

        /// <summary>
        /// Configures whether <see cref="PaginationSet{T}.TotalCountUnfiltered"/> is computed
        /// with a separate (unfiltered) count query. Disabled by default to avoid the extra
        /// round-trip; when disabled, <c>TotalCountUnfiltered</c> equals <c>TotalCount</c>.
        /// Only honored by the convenience overloads that own the unfiltered source query.
        /// </summary>
        public void IncludeUnfilteredCount(bool include = true)
        {
            this.ThrowIfFrozen();
            this.unfilteredCountIncluded = include;
        }

        internal bool UnfilteredCountIncluded => this.unfilteredCountIncluded;

        internal UnknownPropertyHandling UnknownSortPropertyHandling => this.unknownSortPropertyHandling;

        internal UnknownPropertyHandling UnknownFilterPropertyHandling => this.unknownFilterPropertyHandling;

        internal InvalidValueHandling InvalidFilterValueHandling => this.invalidFilterValueHandling;

        internal Func<string, Expression<Func<TEntity, bool>>>? SearchPredicateFactory => this.searchPredicateFactory;

        internal IReadOnlyList<DefaultSortDefinition> DefaultSortDefinitions => this.defaultSortDefinitions;

        internal bool TryGetSortDefinition(string externalName, [MaybeNullWhen(false)] out PropertyDefinition<TEntity> propertyDefinition)
        {
            return this.sortLookup!.TryGetValue(externalName, out propertyDefinition);
        }

        internal bool TryGetFilterDefinition(string externalName, [MaybeNullWhen(false)] out PropertyDefinition<TEntity> propertyDefinition)
        {
            return this.filterLookup!.TryGetValue(externalName, out propertyDefinition);
        }

        /// <summary>
        /// Validates the configuration and freezes this instance.
        /// Called on first use; subsequent calls have no effect.
        /// </summary>
        internal void Freeze()
        {
            if (this.isFrozen)
            {
                return;
            }

            this.Validate();

            this.sortLookup = BuildLookup(this.propertyDefinitions.Where(d => d.IsSortable), this.nameComparer);
            this.filterLookup = BuildLookup(this.propertyDefinitions.Where(d => d.IsFilterable), this.nameComparer);

            this.isFrozen = true;
        }

        /// <summary>
        /// Validates the configuration before this instance is frozen.
        /// </summary>
        protected virtual void Validate()
        {
            foreach (var propertyDefinition in this.propertyDefinitions)
            {
                propertyDefinition.Validate();
            }
        }

        internal void ThrowIfFrozen()
        {
            if (this.isFrozen)
            {
                throw new InvalidOperationException(
                    $"{this.GetType().Name} cannot be configured anymore since it has already been used. " +
                    "Configure the options before the first query is executed.");
            }
        }

        internal void RenameDefinition(PropertyDefinition<TEntity> propertyDefinition, string externalName)
        {
            this.ThrowIfFrozen();
            EnsureValidExternalName(externalName);
            this.EnsureUniqueName(externalName, exclude: propertyDefinition);

            propertyDefinition.ExternalName = externalName;
        }

        private PropertyOptions<TEntity> AddPropertyDefinition(PropertyDefinition<TEntity> propertyDefinition)
        {
            this.EnsureUniqueName(propertyDefinition.ExternalName, exclude: null);
            this.propertyDefinitions.Add(propertyDefinition);

            return new PropertyOptions<TEntity>(this, propertyDefinition);
        }

        private void EnsureUniqueName(string externalName, PropertyDefinition<TEntity>? exclude)
        {
            if (this.propertyDefinitions.Any(d => d != exclude && this.nameComparer.Equals(d.ExternalName, externalName)))
            {
                throw new ArgumentException($"A property with name '{externalName}' is already registered.", nameof(externalName));
            }
        }

        private static void EnsureValidExternalName(string externalName)
        {
            if (string.IsNullOrWhiteSpace(externalName))
            {
                throw new ArgumentException("External property name must not be null or empty.", nameof(externalName));
            }
        }

        private static Dictionary<string, PropertyDefinition<TEntity>> BuildLookup(
            IEnumerable<PropertyDefinition<TEntity>> propertyDefinitions,
            StringComparer comparer)
        {
            var lookup = new Dictionary<string, PropertyDefinition<TEntity>>(comparer);

            foreach (var propertyDefinition in propertyDefinitions)
            {
                if (lookup.ContainsKey(propertyDefinition.ExternalName))
                {
                    throw new ArgumentException($"A property with name '{propertyDefinition.ExternalName}' is already registered.");
                }

                lookup.Add(propertyDefinition.ExternalName, propertyDefinition);
            }

            return lookup;
        }
    }
}
