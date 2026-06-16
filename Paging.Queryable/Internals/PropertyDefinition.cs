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

        internal PropertyDefinition(string externalName, string propertyPath, LambdaExpression? propertyPathLambda = null)
        {
            this.ExternalName = externalName;
            this.PropertyPath = propertyPath;
            this.sortKeySelector = propertyPathLambda;
        }

        internal string ExternalName { get; set; }

        internal string PropertyPath { get; }

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
                this.sortCapability == Capability.PropertyPath && this.sortKeySelector == null ||
                this.filterCapability == Capability.PropertyPath;

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
