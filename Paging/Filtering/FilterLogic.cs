namespace Paging
{
    /// <summary>
    /// Determines how the child nodes of a <see cref="FilterGroup"/> are combined.
    /// </summary>
    public enum FilterLogic
    {
        /// <summary>
        /// All child nodes must match (logical AND).
        /// </summary>
        And = 0,

        /// <summary>
        /// At least one child node must match (logical OR).
        /// </summary>
        Or = 1,
    }
}
