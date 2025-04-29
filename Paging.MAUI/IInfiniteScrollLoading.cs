namespace Paging.MAUI
{
    public interface IInfiniteScrollLoading
    {
        bool IsLoadingMore { get; }

        event EventHandler<LoadingMoreEventArgs> LoadingMore;
    }
}