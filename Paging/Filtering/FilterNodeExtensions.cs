namespace Paging
{
    /// <summary>
    /// Combinators for merging <see cref="FilterNode"/> trees. These null-coalesce (a null side is
    /// dropped) and flatten nested groups of the same logic, so chaining stays a single flat group.
    /// Useful for folding a server-side or out-of-band condition into an incoming request filter
    /// without the verbose null check, e.g. <c>pagingInfo.AndFilter(mandatoryCondition)</c>.
    /// </summary>
    public static class FilterNodeExtensions
    {
        /// <summary>
        /// Combines two filter nodes with <see cref="FilterLogic.And"/>. Returns the non-null side if
        /// one is null, or <c>null</c> if both are null. Nested AND groups are flattened.
        /// </summary>
        public static FilterNode? And(this FilterNode? left, FilterNode? right)
        {
            return Combine(left, right, FilterLogic.And);
        }

        /// <summary>
        /// Combines two filter nodes with <see cref="FilterLogic.Or"/>. Returns the non-null side if
        /// one is null, or <c>null</c> if both are null. Nested OR groups are flattened.
        /// </summary>
        public static FilterNode? Or(this FilterNode? left, FilterNode? right)
        {
            return Combine(left, right, FilterLogic.Or);
        }

        /// <summary>
        /// ANDs the given filter into <see cref="PagingInfo.Filter"/> and returns the same
        /// <see cref="PagingInfo"/> for chaining.
        /// </summary>
        public static PagingInfo AndFilter(this PagingInfo pagingInfo, FilterNode? filter)
        {
            if (pagingInfo == null)
            {
                throw new ArgumentNullException(nameof(pagingInfo));
            }

            pagingInfo.Filter = pagingInfo.Filter.And(filter);
            return pagingInfo;
        }

        /// <summary>
        /// ORs the given filter into <see cref="PagingInfo.Filter"/> and returns the same
        /// <see cref="PagingInfo"/> for chaining.
        /// </summary>
        public static PagingInfo OrFilter(this PagingInfo pagingInfo, FilterNode? filter)
        {
            if (pagingInfo == null)
            {
                throw new ArgumentNullException(nameof(pagingInfo));
            }

            pagingInfo.Filter = pagingInfo.Filter.Or(filter);
            return pagingInfo;
        }

        private static FilterNode? Combine(FilterNode? left, FilterNode? right, FilterLogic logic)
        {
            if (left == null)
            {
                return right;
            }

            if (right == null)
            {
                return left;
            }

            var nodes = new List<FilterNode>();
            AddNodes(nodes, left, logic);
            AddNodes(nodes, right, logic);
            return new FilterGroup { Logic = logic, Nodes = nodes };
        }

        private static void AddNodes(List<FilterNode> nodes, FilterNode node, FilterLogic logic)
        {
            if (node is FilterGroup group && group.Logic == logic)
            {
                nodes.AddRange(group.Nodes);
            }
            else
            {
                nodes.Add(node);
            }
        }
    }
}
