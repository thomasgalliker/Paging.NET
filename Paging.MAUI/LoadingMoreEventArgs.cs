using System;

namespace Paging.MAUI
{
    public class LoadingMoreEventArgs : EventArgs
    {
        public LoadingMoreEventArgs(bool isLoadingMore)
        {
            this.IsLoadingMore = isLoadingMore;
        }

        public bool IsLoadingMore { get; }
    }
}
