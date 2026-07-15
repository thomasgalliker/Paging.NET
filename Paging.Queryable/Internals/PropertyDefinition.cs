using System.Linq.Expressions;

namespace Paging.Queryable.Internals
{
    /// <summary>
    /// Holds the registration of a single property:
    /// the external name, the property path and the declared
    /// sort/filter capabilities including optional custom expressions.
    /// </summary>
    internal sealed class PropertyDefinition<TEntity>
    {
        private enum Capability
        {
            None,
            PropertyPath,
            Custom,
            CustomFactory,
        }

        private Capability sortCapability;
        private LambdaExpression? sortKeySelector;
        private Func<LambdaExpression>? sortKeySelectorFactory;

        private Capability filterCapability;
        private Func<FilterOperator, object?, Expression<Func<TEntity, bool>>?>? filterPredicateFactory;
        private LambdaExpression? filterPropertyLambda;

        private readonly LambdaExpression? collectionSelector;
        private readonly LambdaExpression? elementSelector;

        internal PropertyDefinition(string externalName, string propertyPath, LambdaExpression? propertyPathLambda = null)
        {
            this.ExternalName = externalName;
            this.PropertyPath = propertyPath;
            this.sortKeySelector = propertyPathLambda;
        }

        internal PropertyDefinition(string externalName, string propertyPath, LambdaExpression collectionSelector, LambdaExpression elementSelector)
        {
            this.ExternalName = externalName;
            this.PropertyPath = propertyPath;
            this.collectionSelector = collectionSelector;
            this.elementSelector = elementSelector;
        }

        internal string ExternalName { get; set; }

        internal string PropertyPath { get; }

        /// <summary>
        /// Whether this registration maps to a to-many navigation (filtered via <c>.Any(...)</c>).
        /// </summary>
        internal bool IsCollectionFilter => this.collectionSelector != null;

        internal LambdaExpression? CollectionSelector => this.collectionSelector;

        internal LambdaExpression? ElementSelector => this.elementSelector;

        internal bool IsSortable => this.sortCapability != Capability.None;

        internal bool IsFilterable => this.filterCapability != Capability.None;

        internal bool HasCustomFilter => this.filterCapability == Capability.Custom;

        internal Expression<Func<TEntity, bool>>? BuildCustomPredicate(FilterOperator filterOperator, object? value)
        {
            return this.filterPredicateFactory!(filterOperator, value);
        }

        internal void DeclareSortable()
        {
            this.EnsureNotYetSortable();
            this.sortCapability = Capability.PropertyPath;
        }

        internal void DeclareSortable(LambdaExpression keySelector)
        {
            this.EnsureNotYetSortable();
            this.sortCapability = Capability.Custom;
            this.sortKeySelector = keySelector;
        }

        internal void DeclareSortable(Func<LambdaExpression> keySelectorFactory)
        {
            this.EnsureNotYetSortable();
            this.sortCapability = Capability.CustomFactory;
            this.sortKeySelectorFactory = keySelectorFactory;
        }

        internal void DeclareFilterable()
        {
            this.EnsureNotYetFilterable();
            this.filterCapability = Capability.PropertyPath;
        }

        internal void DeclareFilterable(Func<object?, Expression<Func<TEntity, bool>>?> predicateFactory)
        {
            this.DeclareFilterable((_, value) => predicateFactory(value));
        }

        internal void DeclareFilterable(Func<FilterOperator, object?, Expression<Func<TEntity, bool>>?> predicateFactory)
        {
            if (this.IsCollectionFilter)
            {
                throw new InvalidOperationException(
                    $"Property '{this.ExternalName}' maps to a collection and does not support a custom filter predicate. " +
                    "Collection registrations filter via the built-in Any(...) comparison.");
            }

            this.EnsureNotYetFilterable();
            this.filterCapability = Capability.Custom;
            this.filterPredicateFactory = predicateFactory;
        }

        internal LambdaExpression GetSortKeySelector()
        {
            if (this.sortCapability == Capability.CustomFactory)
            {
                return this.sortKeySelectorFactory!();
            }

            return this.sortKeySelector ??= SortExpressionBuilder.CreatePropertyPathLambda<TEntity>(this.PropertyPath)
                ?? throw new PagingException(
                    $"Property path '{this.PropertyPath}' cannot be resolved on type '{typeof(TEntity).Name}'.",
                    this.ExternalName);
        }

        /// <summary>
        /// Returns the (cached) property-path lambda used to build a filter predicate.
        /// Built once per registration since <see cref="PagingOptions{TEntity}"/> is frozen and reused across queries.
        /// </summary>
        internal LambdaExpression GetFilterPropertyLambda()
        {
            return this.filterPropertyLambda ??= SortExpressionBuilder.CreatePropertyPathLambda<TEntity>(this.PropertyPath)
                ?? throw new PagingException(
                    $"Property path '{this.PropertyPath}' cannot be resolved on type '{typeof(TEntity).Name}'.",
                    this.ExternalName);
        }

        /// <summary>
        /// Validates this registration when the owning <see cref="PagingOptions{TEntity}"/> is frozen.
        /// </summary>
        internal void Validate()
        {
            if (!this.IsSortable && !this.IsFilterable)
            {
                throw new InvalidOperationException(
                    $"Property '{this.ExternalName}' declares no capabilities. " +
                    $"Call Sortable() and/or Filterable() on the property registration.");
            }

            var usesPropertyPath =
                !this.IsCollectionFilter &&
                (this.sortCapability == Capability.PropertyPath && this.sortKeySelector == null ||
                 this.filterCapability == Capability.PropertyPath);

            if (usesPropertyPath && SortExpressionBuilder.CreatePropertyPathLambda<TEntity>(this.PropertyPath) == null)
            {
                throw new PagingException(
                    $"Property '{this.ExternalName}' is registered with property path '{this.PropertyPath}' " +
                    $"which cannot be resolved on type '{typeof(TEntity).Name}'.",
                    this.ExternalName);
            }
        }

        private void EnsureNotYetSortable()
        {
            if (this.IsCollectionFilter)
            {
                throw new InvalidOperationException(
                    $"Property '{this.ExternalName}' maps to a collection and cannot be declared as sortable. " +
                    "Collection registrations support filtering only.");
            }

            if (this.IsSortable)
            {
                throw new InvalidOperationException($"Property '{this.ExternalName}' is already declared as sortable.");
            }
        }

        private void EnsureNotYetFilterable()
        {
            if (this.IsFilterable)
            {
                throw new InvalidOperationException($"Property '{this.ExternalName}' is already declared as filterable.");
            }
        }
    }
}
