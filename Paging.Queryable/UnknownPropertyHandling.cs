namespace Paging.Queryable
{
    /// <summary>
    /// Determines how sort and filter property names are handled
    /// which are not registered in <see cref="PagingOptions{TEntity}"/>.
    /// </summary>
    public enum UnknownPropertyHandling
    {
        /// <summary>
        /// Throws a <see cref="PagingException"/> when an unknown property name is encountered.
        /// This is the default behavior. Web APIs typically translate this exception
        /// into an HTTP 400 (Bad Request) response.
        /// </summary>
        Throw = 0,

        /// <summary>
        /// Silently skips unknown property names.
        /// </summary>
        Ignore,
    }
}
