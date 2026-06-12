namespace Paging.Queryable.Tests.TestData
{
    /// <summary>
    /// Abstraction for the system clock, as commonly used in real-world backends
    /// to keep time-dependent logic testable.
    /// </summary>
    public interface IDateTime
    {
        DateTime UtcNow { get; }
    }
}
