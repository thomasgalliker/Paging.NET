using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Paging.MAUI
{
    /// <summary>
    /// A collection supporting incremental data loading for infinite scrolling scenarios.
    /// <para>
    /// Pass a page loader to the constructor to let the collection own the <see cref="PagingInfo"/>,
    /// advance the page after each load and derive <see cref="CanLoadMore"/> from
    /// <see cref="PaginationSet{T}.HasMorePages"/>. Alternatively, use the parameterless constructor and
    /// wire <see cref="OnLoadMore"/>/<see cref="OnCanLoadMore"/> manually.
    /// </para>
    /// </summary>
    /// <typeparam name="T">The type of items contained in the collection.</typeparam>
    public class InfiniteScrollCollection<T> : ObservableCollection<T>, IInfiniteScrollLoader, IInfiniteScrollLoading
    {
        private bool isLoadingMore;
        private Func<bool>? lastSetHasMorePages;

        /// <summary>
        /// Initializes a new instance of the collection.
        /// </summary>
        public InfiniteScrollCollection()
        {
        }

        /// <summary>
        /// Initializes a new instance of the collection with the specified items.
        /// </summary>
        /// <param name="collection">Initial items to populate the collection.</param>
        public InfiniteScrollCollection(IEnumerable<T> collection)
            : base(collection)
        {
        }

        /// <summary>
        /// Initializes a new self-contained collection that loads pages through <paramref name="pageLoader"/>.
        /// The collection owns <paramref name="pagingInfo"/>, advances the page after each load and derives
        /// <see cref="CanLoadMore"/> from <see cref="PaginationSet{T}.HasMorePages"/>. Call
        /// <see cref="InitializeAsync"/> once after construction to load the first page.
        /// </summary>
        /// <param name="pageLoader">
        /// Loads a single page for the given <see cref="PagingInfo"/>. The returned <see cref="PaginationSet{T}"/>
        /// must carry accurate <see cref="PaginationSet{T}.CurrentPage"/> and <see cref="PaginationSet{T}.TotalPages"/>.
        /// </param>
        /// <param name="pagingInfo">The initial paging request. A new <see cref="PagingInfo"/> is used when <c>null</c>.</param>
        public InfiniteScrollCollection(
            Func<PagingInfo, Task<PaginationSet<T>>> pageLoader,
            PagingInfo? pagingInfo = null)
        {
            ArgumentNullException.ThrowIfNull(pageLoader);

            this.PagingInfo = pagingInfo ?? new PagingInfo();
            this.OnCanLoadMore = () => this.lastSetHasMorePages?.Invoke() ?? true;
            this.OnLoadMore = async () =>
            {
                var paginationSet = await pageLoader(this.PagingInfo);

                // Materialize before recording any state. When a derived collection composes a projection
                // into the loader, the projection runs here; a fault then leaves PagingInfo.CurrentPage and
                // the can-load-more flag untouched, so the same page is retried cleanly rather than skipped.
                var items = paginationSet.Items.ToArray();

                this.lastSetHasMorePages = paginationSet.HasMorePages;
                if (paginationSet.HasMorePages())
                {
                    this.PagingInfo.CurrentPage++;
                }

                return items;
            };
        }

        /// <summary>
        /// Gets the paging request advanced as pages are loaded. Mutate properties such as
        /// <see cref="PagingInfo.Search"/>, <see cref="PagingInfo.Filter"/> or <see cref="PagingInfo.SortBy"/>
        /// and call <see cref="RefreshAsync"/> to reload from the first page. For delegate-driven collections
        /// this is an unused default instance.
        /// </summary>
        public PagingInfo PagingInfo { get; private set; } = new PagingInfo();

        /// <summary>
        /// Invoked before loading more items begins.
        /// </summary>
        public Action? OnBeforeLoadMore { get; set; }

        /// <summary>
        /// Invoked after loading more items finishes.
        /// </summary>
        public Action? OnAfterLoadMore { get; set; }

        /// <summary>
        /// Invoked when an exception occurs during data loading.
        /// </summary>
        public Action<Exception>? OnError { get; set; }

        /// <summary>
        /// Determines whether more items can be loaded.
        /// </summary>
        public Func<bool>? OnCanLoadMore { get; set; }

        /// <summary>
        /// Provides the asynchronous data loading operation.
        /// </summary>
        /// <remarks>Must be set before calling <see cref="LoadMoreAsync"/>.</remarks>
        public Func<Task<IEnumerable<T>>>? OnLoadMore { get; set; }

        /// <summary>
        /// Gets a value indicating whether more data can be requested.
        /// </summary>
        public virtual bool CanLoadMore => this.OnCanLoadMore?.Invoke() ?? false;

        /// <summary>
        /// Gets a value indicating whether a load operation is currently in progress.
        /// </summary>
        public bool IsLoadingMore
        {
            get => this.isLoadingMore;
            private set
            {
                if (this.isLoadingMore != value)
                {
                    this.isLoadingMore = value;
                    this.OnPropertyChanged(new PropertyChangedEventArgs(nameof(this.IsLoadingMore)));

                    this.LoadingMore?.Invoke(this, new LoadingMoreEventArgs(this.IsLoadingMore));
                }
            }
        }

        /// <summary>
        /// Occurs when the loading state changes.
        /// </summary>
        public event EventHandler<LoadingMoreEventArgs>? LoadingMore;

        /// <summary>
        /// Loads the next batch of items asynchronously.
        /// </summary>
        /// <returns>A task representing the load operation.</returns>
        public async Task LoadMoreAsync()
        {
            try
            {
                this.IsLoadingMore = true;
                this.OnBeforeLoadMore?.Invoke();

                if (this.OnLoadMore is not Func<Task<IEnumerable<T>>> loadMoreTask)
                {
                    throw new InvalidOperationException($"{nameof(this.OnLoadMore)} must be set before calling LoadMoreAsync.");
                }

                var result = await loadMoreTask();
                if (result != null!)
                {
                    this.AddRange(result);
                }
            }
            catch (Exception ex) when (this.OnError != null)
            {
                this.OnError.Invoke(ex);
            }
            finally
            {
                this.IsLoadingMore = false;
                this.OnAfterLoadMore?.Invoke();
            }
        }

        /// <summary>
        /// Loads the first page. Call this once after construction to populate a self-contained collection.
        /// </summary>
        /// <returns>A task representing the load operation.</returns>
        public Task InitializeAsync()
        {
            return this.LoadMoreAsync();
        }

        /// <summary>
        /// Clears the collection, resets paging to the first page and reloads.
        /// Use this after changing search, filter or sort on <see cref="PagingInfo"/>.
        /// Does nothing while a load is already in progress; in that case call <see cref="RefreshAsync"/>
        /// again once <see cref="IsLoadingMore"/> is <c>false</c> so the new criteria are applied.
        /// </summary>
        /// <returns>A task representing the reload operation.</returns>
        public async Task RefreshAsync()
        {
            // Skip while a load is in flight: clearing here would race the appending load and
            // interleave pages. The refresh is intentionally dropped, not queued (see remarks).
            if (this.IsLoadingMore)
            {
                return;
            }

            this.PagingInfo.CurrentPage = this.PagingInfo.FirstPageIndex;
            this.lastSetHasMorePages = null;
            this.ClearItems();

            await this.LoadMoreAsync();
        }

        /// <summary>
        /// Adds a collection of items to the existing items.
        /// </summary>
        /// <param name="collection">The items to add.</param>
        public void AddRange(IEnumerable<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            // Similar to ObservableRangeCollection we want to efficiently update an ObservableCollection
            // with a new range of items.
            // https://raw.githubusercontent.com/haefele/MatchMaker/dev/src/frontend/MatchMaker.UI/Helpers/ObservableRangeCollection.cs

            this.CheckReentrancy();

            var startIndex = this.Count;
            var changedItems = new List<T>(collection);

            foreach (var i in changedItems)
            {
                this.Items.Add(i);
            }

            this.OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            this.OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, changedItems, startIndex));
        }
    }

    /// <summary>
    /// A self-contained <see cref="InfiniteScrollCollection{TItem}"/> that loads <typeparamref name="TSource"/>
    /// pages, projects each item to <typeparamref name="TItem"/> and appends them — without any per-page wiring
    /// in the consuming code. The collection owns the <see cref="InfiniteScrollCollection{TItem}.PagingInfo"/>,
    /// advances the page after each load and derives <see cref="InfiniteScrollCollection{TItem}.CanLoadMore"/>
    /// from <see cref="PaginationSet{T}.HasMorePages"/>.
    /// <para>
    /// When the loaded type is the type you bind to (no projection), use the single-generic
    /// <see cref="InfiniteScrollCollection{T}"/> with its page-loader constructor instead.
    /// </para>
    /// </summary>
    /// <typeparam name="TSource">The item type returned by the page loader (e.g. an entity or DTO).</typeparam>
    /// <typeparam name="TItem">The item type held by the collection (e.g. a view model).</typeparam>
    public class InfiniteScrollCollection<TSource, TItem> : InfiniteScrollCollection<TItem>
    {
        /// <summary>
        /// Initializes a new self-contained, projecting infinite scroll collection. Call
        /// <see cref="InfiniteScrollCollection{TItem}.InitializeAsync"/> once after construction to load the first page.
        /// </summary>
        /// <param name="pageLoader">
        /// Loads a single page for the given <see cref="PagingInfo"/>. The returned <see cref="PaginationSet{T}"/>
        /// must carry accurate <see cref="PaginationSet{T}.CurrentPage"/> and <see cref="PaginationSet{T}.TotalPages"/>.
        /// </param>
        /// <param name="itemSelector">Maps each loaded <typeparamref name="TSource"/> item to a <typeparamref name="TItem"/>.</param>
        /// <param name="pagingInfo">The initial paging request. A new <see cref="PagingInfo"/> is used when <c>null</c>.</param>
        public InfiniteScrollCollection(
            Func<PagingInfo, Task<PaginationSet<TSource>>> pageLoader,
            Func<TSource, TItem> itemSelector,
            PagingInfo? pagingInfo = null)
            : base(Project(pageLoader, itemSelector), pagingInfo)
        {
        }

        /// <summary>
        /// Composes <paramref name="pageLoader"/> and <paramref name="itemSelector"/> into a single loader that
        /// returns a <see cref="PaginationSet{TItem}"/>, so the base self-contained constructor can consume it.
        /// The projection is applied lazily by <see cref="PaginationSetExtensions.Map"/>, keeping all paging
        /// metadata and deferring the mapping until the base materializes the page.
        /// </summary>
        private static Func<PagingInfo, Task<PaginationSet<TItem>>> Project(
            Func<PagingInfo, Task<PaginationSet<TSource>>> pageLoader,
            Func<TSource, TItem> itemSelector)
        {
            ArgumentNullException.ThrowIfNull(pageLoader);
            ArgumentNullException.ThrowIfNull(itemSelector);

            return async pagingInfo => (await pageLoader(pagingInfo)).Map(items => items.Select(itemSelector));
        }
    }
}