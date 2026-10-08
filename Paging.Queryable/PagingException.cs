namespace Paging.Queryable
{
    /// <summary>
    /// Thrown when a paging request cannot be applied to the query,
    /// e.g. when an unknown sort or filter property is requested
    /// and <see cref="UnknownPropertyHandling.Throw"/> is configured.
    /// </summary>
    public class PagingException : Exception
    {
        public PagingException(string message, string? propertyName = null)
            : base(message)
        {
            this.PropertyName = propertyName;
        }

        public PagingException(string message, string? propertyName, Exception? innerException)
            : base(message, innerException)
        {
            this.PropertyName = propertyName;
        }

        /// <summary>
        /// The external property name which caused this exception, if any.
        /// </summary>
        public string? PropertyName { get; }
    }
}
