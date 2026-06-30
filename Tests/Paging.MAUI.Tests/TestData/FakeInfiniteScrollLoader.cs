namespace Paging.MAUI.Tests.TestData
{
    /// <summary>
    /// A controllable <see cref="IInfiniteScrollLoader"/> used to exercise <c>InfiniteScrollBehavior</c>
    /// independently of <see cref="InfiniteScrollCollection{T}"/>. Backed by a <see cref="List{T}"/> so it can
    /// serve as the bound <c>ItemsSource</c> while letting tests control CanLoadMore and the load operation.
    /// </summary>
    internal sealed class FakeInfiniteScrollLoader : List<CarViewModel>, IInfiniteScrollLoader, IInfiniteScrollLoading
    {
        private bool isLoadingMore;

        public Func<bool> CanLoadMoreFunc { get; set; } = () => false;

        public Func<Task<IEnumerable<CarViewModel>>>? OnLoadMore { get; set; }

        public bool CanLoadMore => this.CanLoadMoreFunc();

        public bool IsLoadingMore
        {
            get => this.isLoadingMore;
            private set
            {
                if (this.isLoadingMore != value)
                {
                    this.isLoadingMore = value;
                    this.LoadingMore?.Invoke(this, new LoadingMoreEventArgs(value));
                }
            }
        }

        public event EventHandler<LoadingMoreEventArgs>? LoadingMore;

        public async Task LoadMoreAsync()
        {
            this.IsLoadingMore = true;
            try
            {
                if (this.OnLoadMore != null)
                {
                    var items = await this.OnLoadMore();
                    this.AddRange(items);
                }
            }
            finally
            {
                this.IsLoadingMore = false;
            }
        }
    }
}
