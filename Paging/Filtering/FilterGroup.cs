namespace Paging
{
    /// <summary>
    /// Combines multiple <see cref="FilterNode"/> children with a logical
    /// <see cref="FilterLogic.And"/> or <see cref="FilterLogic.Or"/>. Groups can be nested
    /// to express arbitrary boolean filter trees, e.g. <c>(A AND B) OR C</c>.
    /// </summary>
    public sealed class FilterGroup : FilterNode
    {
        public FilterGroup()
        {
            this.Nodes = new List<FilterNode>();
        }

        public FilterGroup(FilterLogic logic, params FilterNode[] nodes)
        {
            this.Logic = logic;
            this.Nodes = nodes?.ToList() ?? new List<FilterNode>();
        }

        /// <summary>
        /// Creates an AND group of the given child nodes.
        /// </summary>
        public static FilterGroup And(params FilterNode[] nodes)
        {
            return new FilterGroup(FilterLogic.And, nodes);
        }

        /// <summary>
        /// Creates an OR group of the given child nodes.
        /// </summary>
        public static FilterGroup Or(params FilterNode[] nodes)
        {
            return new FilterGroup(FilterLogic.Or, nodes);
        }

        /// <summary>
        /// How the child <see cref="Nodes"/> are combined.
        /// </summary>
        public FilterLogic Logic { get; set; }

        /// <summary>
        /// The child nodes (conditions or nested groups).
        /// </summary>
        public IList<FilterNode> Nodes { get; set; }

        public override bool Equals(FilterNode? other)
        {
            if (other is not FilterGroup group ||
                this.Logic != group.Logic ||
                this.Nodes.Count != group.Nodes.Count)
            {
                return false;
            }

            for (var i = 0; i < this.Nodes.Count; i++)
            {
                if (!Equals(this.Nodes[i], group.Nodes[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)this.Logic;
                foreach (var node in this.Nodes)
                {
                    hashCode = (hashCode * 397) ^ (node != null ? node.GetHashCode() : 0);
                }

                return hashCode;
            }
        }

        public override string ToString()
        {
            return FilterExpressionWriter.Write(this);
        }
    }
}
