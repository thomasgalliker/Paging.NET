using System;

namespace Paging.Forms
{
    public interface IInfiniteScrollLoading
    {
        bool IsLoadingMore { get; }

        event EventHandler<LoadingMoreEventArgs> LoadingMore;
    }
}
