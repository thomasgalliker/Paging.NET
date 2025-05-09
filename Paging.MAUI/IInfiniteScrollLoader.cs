namespace Paging.MAUI
{
    public interface IInfiniteScrollLoader
    {
        bool CanLoadMore { get; }

        Task LoadMoreAsync();
    }
}