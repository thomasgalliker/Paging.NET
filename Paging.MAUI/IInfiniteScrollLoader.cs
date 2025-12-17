namespace Paging.MAUI
{
    /// <summary>
    /// Represents a component capable of incrementally loading paged data,
    /// commonly used for infinite scrolling scenarios.
    /// </summary>
    public interface IInfiniteScrollLoader
    {
        /// <summary>
        /// Gets a value indicating whether additional data can be requested.
        /// </summary>
        /// <remarks>
        /// This property should return <c>true</c> when more data is
        /// available to load, and <c>false</c> when there is no
        /// additional content remaining or data loading has been completed.
        /// </remarks>
        bool CanLoadMore { get; }

        /// <summary>
        /// Asynchronously loads the next page of data.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous load operation.
        /// </returns>
        /// <remarks>
        /// Implementations should update <see cref="CanLoadMore"/> accordingly
        /// after the load operation completes.
        /// </remarks>
        Task LoadMoreAsync();
    }

}