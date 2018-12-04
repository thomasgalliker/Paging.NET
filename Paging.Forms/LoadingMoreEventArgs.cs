using System;

namespace Paging.Forms
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
