using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Paging.MAUI
{
    /// <summary>
    /// A collection supporting incremental data loading for infinite scrolling scenarios.
    /// <para>
    /// Construct with a <see cref="PagingInfo"/> and a page loader. When the loaded type already is the bound type,
    /// pass the loader to <c>WithPageLoader</c> and you are done; to project a different loaded type onto the
    /// collection use <see cref="WithPageLoader{TSource}"/> followed by <c>WithMapping</c> (per item, or a whole
    /// page at once). The collection then owns the
    /// <see cref="PagingInfo"/>, advances the page after each load and derives <see cref="CanLoadMore"/> from
    /// <see cref="PaginationSet{T}.HasMorePages"/>. Call <see cref="InitializeAsync"/> once to load the first page.
    /// </para>
    /// </summary>
    /// <typeparam name="TTarget">The type of items contained in the collection.</typeparam>
    public class InfiniteScrollCollection<TTarget> : ObservableCollection<TTarget>, IInfiniteScrollLoader, IInfiniteScrollLoading
    {
        private bool isLoadingMore;
        private PaginationSet<TTarget>? lastPaginationSet;
        private Func<PagingInfo, Task<PaginationSet<TTarget>>>? pageLoader;
        private bool pendingProjection;
        private Action? onBeforeLoadMore;
        private Action? onAfterLoadMore;
        private Action<Exception>? onError;
        private bool refreshPending;
        private bool isDraining;
        private TaskCompletionSource? drainCompletion;

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
        public InfiniteScrollCollection(IEnumerable<TTarget> collection)
            : base(collection)
        {
        }

        /// <summary>
        /// Initializes a new self-contained collection that is configured fluently. Follow construction with a page
        /// loader: when the loaded type already is <typeparamref name="TTarget"/>, pass it to <c>WithPageLoader</c>
        /// directly; otherwise use <see cref="WithPageLoader{TSource}"/> and then <c>WithMapping</c> (per item, or a
        /// whole page at once) to project the loaded pages to
        /// <typeparamref name="TTarget"/>. Then call <see cref="InitializeAsync"/> once to load the first page. The
        /// collection owns <paramref name="pagingInfo"/>, advances the page after each load and derives
        /// <see cref="CanLoadMore"/> from <see cref="PaginationSet{T}.HasMorePages"/>.
        /// </summary>
        /// <param name="pagingInfo">The initial paging request. A new <see cref="PagingInfo"/> is used when <c>null</c>.</param>
        public InfiniteScrollCollection(PagingInfo? pagingInfo)
        {
            this.PagingInfo = pagingInfo ?? new PagingInfo();
        }

        /// <summary>
        /// Configures a page loader whose loaded item type already is <typeparamref name="TTarget"/>, so no projection
        /// is required: the loaded items are bound directly and the collection is fully configured and returned. This
        /// overload is selected automatically when the loader's <see cref="PaginationSet{T}"/> is of
        /// <typeparamref name="TTarget"/>; to project a different loaded type, use <see cref="WithPageLoader{TSource}"/>
        /// together with a mapping instead.
        /// </summary>
        /// <param name="pageLoader">
        /// Loads a single page for the given <see cref="PagingInfo"/>. The returned <see cref="PaginationSet{T}"/>
        /// must carry accurate <see cref="PaginationSet{T}.CurrentPage"/> and <see cref="PaginationSet{T}.TotalPages"/>.
        /// </param>
        /// <returns>This collection, for fluent chaining.</returns>
        public InfiniteScrollCollection<TTarget> WithPageLoader(Func<PagingInfo, Task<PaginationSet<TTarget>>> pageLoader)
        {
            ArgumentNullException.ThrowIfNull(pageLoader);

            this.UsePageLoader(pageLoader);
            return this;
        }

        /// <summary>
        /// Begins configuring a projecting page loader for when the loaded type differs from
        /// <typeparamref name="TTarget"/>. <typeparamref name="TSource"/> is the item type the loader returns; chain
        /// <c>WithMapping</c> on the result — per item, or a whole page at once (asynchronous and batched) — to project
        /// each loaded page to <typeparamref name="TTarget"/>. When the loaded type already is
        /// <typeparamref name="TTarget"/>, use the non-generic <c>WithPageLoader</c> overload, which needs no mapping.
        /// </summary>
        /// <param name="pageLoader">
        /// Loads a single page for the given <see cref="PagingInfo"/>. The returned <see cref="PaginationSet{TSource}"/>
        /// must carry accurate <see cref="PaginationSet{T}.CurrentPage"/> and <see cref="PaginationSet{T}.TotalPages"/>.
        /// </param>
        /// <returns>A source that maps the loaded pages onto this collection.</returns>
        public InfiniteScrollSource<TSource, TTarget> WithPageLoader<TSource>(Func<PagingInfo, Task<PaginationSet<TSource>>> pageLoader)
        {
            ArgumentNullException.ThrowIfNull(pageLoader);

            if (typeof(TSource) == typeof(TTarget))
            {
                // The loaded type already is the bound type, so a mapping is optional even on this generic overload
                // (only reached when the type argument is given explicitly; otherwise the non-generic overload wins).
                // Bind directly so the collection is configured without a mapping. The cast is safe: identical type
                // arguments make the two delegate types the same constructed type at runtime.
                this.UsePageLoader((Func<PagingInfo, Task<PaginationSet<TTarget>>>)(object)pageLoader);
            }
            else
            {
                // A projection is required but not configured until a WithMapping overload calls UsePageLoader.
                // Track that a projecting loader was begun so an unfinalized chain fails loudly on first load
                // instead of silently doing nothing (see LoadMoreAsync).
                this.pendingProjection = true;
            }

            return new InfiniteScrollSource<TSource, TTarget>(this, pageLoader);
        }

        internal void UsePageLoader(Func<PagingInfo, Task<PaginationSet<TTarget>>> pageLoader)
        {
            this.pageLoader = pageLoader;
            this.pendingProjection = false;
        }

        /// <summary>
        /// Gets the paging request advanced as pages are loaded. Mutate properties such as
        /// <see cref="PagingInfo.Search"/>, <see cref="PagingInfo.Filter"/> or <see cref="PagingInfo.SortBy"/>
        /// and call <see cref="RefreshAsync"/> to reload from the first page. For delegate-driven collections
        /// this is an unused default instance.
        /// </summary>
        public PagingInfo PagingInfo { get; private set; } = new PagingInfo();

        /// <summary>
        /// Gets the most recently loaded page's <see cref="PaginationSet{T}"/>, or <c>null</c> before the first
        /// load and after <see cref="RefreshAsync"/> resets the collection. Carries the server-side totals
        /// (<see cref="PaginationSet{T}.TotalCount"/> / <see cref="PaginationSet{T}.TotalCountUnfiltered"/>) so
        /// callers can derive empty-state without tracking the loaded pages themselves. Note this is distinct from
        /// <see cref="Collection{T}.Count"/>, which is the number of items loaded so far. Populated only by the
        /// self-contained page-loader path; <c>null</c> for delegate-driven collections.
        /// </summary>
        public PaginationSet<TTarget>? LastPaginationSet
        {
            get => this.lastPaginationSet;
            private set
            {
                this.lastPaginationSet = value;
                this.OnPropertyChanged(new PropertyChangedEventArgs(nameof(this.LastPaginationSet)));
            }
        }

        /// <summary>
        /// Sets the handler invoked before loading more items begins, and returns this collection for fluent chaining.
        /// </summary>
        /// <param name="onBeforeLoadMore">The handler invoked before a load begins.</param>
        /// <returns>This collection, for fluent chaining.</returns>
        public InfiniteScrollCollection<TTarget> OnBeforeLoadMore(Action onBeforeLoadMore)
        {
            ArgumentNullException.ThrowIfNull(onBeforeLoadMore);

            this.onBeforeLoadMore = onBeforeLoadMore;
            return this;
        }

        /// <summary>
        /// Sets the handler invoked after loading more items finishes, and returns this collection for fluent chaining.
        /// </summary>
        /// <param name="onAfterLoadMore">The handler invoked after a load finishes.</param>
        /// <returns>This collection, for fluent chaining.</returns>
        public InfiniteScrollCollection<TTarget> OnAfterLoadMore(Action onAfterLoadMore)
        {
            ArgumentNullException.ThrowIfNull(onAfterLoadMore);

            this.onAfterLoadMore = onAfterLoadMore;
            return this;
        }

        /// <summary>
        /// Sets the handler invoked when a load operation throws, and returns this collection for fluent chaining.
        /// Setting a handler causes load exceptions to be reported to it instead of propagating, so set one to
        /// observe failures in the fire-and-forget first load and in scroll-driven loads (which the behavior runs
        /// as async void).
        /// </summary>
        /// <param name="onError">The handler invoked with the exception when a load fails.</param>
        /// <returns>This collection, for fluent chaining.</returns>
        public InfiniteScrollCollection<TTarget> OnError(Action<Exception> onError)
        {
            ArgumentNullException.ThrowIfNull(onError);

            this.onError = onError;
            return this;
        }

        /// <summary>
        /// Gets a value indicating whether more data can be requested. <c>false</c> until a page loader is
        /// configured and the first page has been loaded; afterwards it reflects whether the most recently loaded
        /// page reports further pages (<see cref="PaginationSet{T}.HasMorePages"/>). It is intentionally
        /// <c>false</c> before the first load so the scroll behavior does not race ahead of
        /// <see cref="InitializeAsync"/>, which loads the first page itself and is not gated by this property.
        /// </summary>
        public virtual bool CanLoadMore => this.pageLoader != null && (this.LastPaginationSet?.HasMorePages() ?? false);

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
            if (this.pageLoader is null)
            {
                if (this.pendingProjection)
                {
                    // WithPageLoader<TSource> was called but the chain was never finalized with a WithMapping
                    // overload, so there is nothing to load.
                    throw new InvalidOperationException(
                        $"A page loader was configured via {nameof(WithPageLoader)}<TSource> but the projection " +
                        $"was never finalized. Chain a WithMapping(...) overload to map the loaded pages to " +
                        $"'{typeof(TTarget).Name}'.");
                }

                // No page loader configured (e.g. an empty or seeded collection): nothing to load.
                return;
            }

            if (this.IsLoadingMore)
            {
                // A load is already in flight. Ignore the overlapping request rather than double-advancing the
                // page and appending the same page twice. Refreshes that need to run after the in-flight load
                // use RefreshAsync, which queues and coalesces them.
                return;
            }

            try
            {
                this.IsLoadingMore = true;
                this.onBeforeLoadMore?.Invoke();

                var paginationSet = await this.pageLoader(this.PagingInfo);

                // Materialize before recording any state. When a projection is composed into the loader, the
                // projection runs here; a fault then leaves PagingInfo.CurrentPage and the last-page metadata
                // untouched, so the same page is retried cleanly rather than skipped.
                var items = paginationSet.Items.ToArray();

                this.LastPaginationSet = paginationSet;
                if (paginationSet.HasMorePages())
                {
                    this.PagingInfo.CurrentPage++;
                }

                this.AddRange(items);
            }
            catch (Exception ex) when (this.onError != null)
            {
                try
                {
                    this.onError.Invoke(ex);
                }
                catch
                {
                    // A faulting error handler must not crash the load path, which the scroll behavior runs as
                    // async void (an unhandled exception there would tear down the app). There is no logger in
                    // this library, so the handler's own exception is intentionally swallowed.
                }
            }
            finally
            {
                this.IsLoadingMore = false;
                this.onAfterLoadMore?.Invoke();
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
        /// Clears the collection, resets paging to the first page and reloads. Use this after changing search,
        /// filter or sort on <see cref="PagingInfo"/>.
        /// <para>
        /// If a load is already in progress (for example a scroll-driven page load), the refresh is queued and
        /// runs once that load completes rather than being dropped. Concurrent refresh requests are coalesced into
        /// a single reload that uses the latest criteria. The returned task completes when the resulting reload has
        /// finished, so callers can drive busy state by awaiting it.
        /// </para>
        /// </summary>
        /// <returns>A task that completes when the (possibly queued) reload finishes.</returns>
        public Task RefreshAsync()
        {
            this.refreshPending = true;

            if (this.isDraining)
            {
                // A drain is already running; it will pick up the pending refresh on its next iteration. All
                // callers await the same drain so they complete together when its final reload finishes.
                return this.drainCompletion!.Task;
            }

            this.isDraining = true;
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            this.drainCompletion = completion;

            _ = this.DrainRefreshAsync(completion);

            // Return the local reference: the drain may already have completed synchronously and nulled the field.
            return completion.Task;
        }

        private async Task DrainRefreshAsync(TaskCompletionSource completion)
        {
            try
            {
                // Wait out any load currently in flight (e.g. a scroll-driven page load) so the clear below does
                // not race the appending load and interleave pages.
                await this.WhenNotLoadingMoreAsync();

                // Coalesce: a refresh requested while the reload below runs collapses into one more iteration
                // rather than queuing a separate reload per request.
                while (this.refreshPending)
                {
                    this.refreshPending = false;

                    this.PagingInfo.CurrentPage = this.PagingInfo.FirstPageIndex;
                    this.LastPaginationSet = null;
                    this.ClearItems();

                    try
                    {
                        await this.LoadMoreAsync();
                    }
                    catch
                    {
                        // The reload faulted and propagated (no onError handler). Re-arm so a refresh that was
                        // coalesced during this reload — or a later retry — is honored by the next drain instead
                        // of being silently consumed, then surface the fault to the awaiters.
                        this.refreshPending = true;
                        throw;
                    }
                }

                completion.TrySetResult();
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
            finally
            {
                this.isDraining = false;
                this.drainCompletion = null;
            }
        }

        /// <summary>
        /// Completes once no load is in progress. Subscribes to <see cref="LoadingMore"/> so a queued refresh can
        /// wait for an in-flight load to finish without polling.
        /// </summary>
        private async Task WhenNotLoadingMoreAsync()
        {
            while (this.IsLoadingMore)
            {
                var loadingMoreFinished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

                void OnLoadingMore(object? sender, LoadingMoreEventArgs e)
                {
                    if (!e.IsLoadingMore)
                    {
                        loadingMoreFinished.TrySetResult();
                    }
                }

                this.LoadingMore += OnLoadingMore;
                try
                {
                    // Re-check after subscribing so a completion that raced the subscription is not missed.
                    if (this.IsLoadingMore)
                    {
                        await loadingMoreFinished.Task;
                    }
                }
                finally
                {
                    this.LoadingMore -= OnLoadingMore;
                }
            }
        }

        /// <summary>
        /// Adds a collection of items to the existing items.
        /// </summary>
        /// <param name="collection">The items to add.</param>
        public void AddRange(IEnumerable<TTarget> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            // Similar to ObservableRangeCollection we want to efficiently update an ObservableCollection
            // with a new range of items.
            // https://raw.githubusercontent.com/haefele/MatchMaker/dev/src/frontend/MatchMaker.UI/Helpers/ObservableRangeCollection.cs

            var changedItems = new List<TTarget>(collection);
            if (changedItems.Count == 0)
            {
                // Nothing to add: suppress a spurious Add notification for an empty page (e.g. a zero-result load).
                return;
            }

            this.CheckReentrancy();

            var startIndex = this.Count;

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
    /// Configures how the pages loaded by an <see cref="InfiniteScrollCollection{TTarget}"/>'s page loader are
    /// projected to <typeparamref name="TTarget"/>. Obtained from
    /// <see cref="InfiniteScrollCollection{TTarget}.WithPageLoader{TSource}"/> and finalized with a <c>WithMapping</c>
    /// overload — per item, or a whole page at once (asynchronous and batched) — each of which returns the
    /// configured collection.
    /// </summary>
    /// <typeparam name="TSource">The item type returned by the page loader (e.g. an entity or DTO).</typeparam>
    /// <typeparam name="TTarget">The item type held by the collection (e.g. a view model).</typeparam>
    public readonly struct InfiniteScrollSource<TSource, TTarget>
    {
        private readonly InfiniteScrollCollection<TTarget> collection;
        private readonly Func<PagingInfo, Task<PaginationSet<TSource>>> pageLoader;

        internal InfiniteScrollSource(
            InfiniteScrollCollection<TTarget> collection,
            Func<PagingInfo, Task<PaginationSet<TSource>>> pageLoader)
        {
            this.collection = collection;
            this.pageLoader = pageLoader;
        }

        /// <summary>
        /// Projects each loaded <typeparamref name="TSource"/> item to a <typeparamref name="TTarget"/> and finalizes
        /// the collection.
        /// </summary>
        /// <param name="itemSelector">Maps a single loaded item to the bound item type.</param>
        /// <returns>The configured collection, for fluent chaining.</returns>
        public InfiniteScrollCollection<TTarget> WithMapping(Func<TSource, TTarget> itemSelector)
        {
            ArgumentNullException.ThrowIfNull(itemSelector);

            var pageLoader = this.pageLoader;

            // Materialize the projection (ToList) so LastPaginationSet.Items holds the same TTarget instances the
            // collection was populated with. A lazy Select would re-run itemSelector on every re-enumeration of
            // LastPaginationSet.Items, producing duplicate instances distinct from the bound items.
            this.collection.UsePageLoader(async pagingInfo => (await pageLoader(pagingInfo)).Map(items => items.Select(itemSelector).ToList()));

            return this.collection;
        }

        /// <summary>
        /// Projects each loaded page asynchronously and as a whole, then finalizes the collection. This per-page
        /// overload of <c>WithMapping</c> takes the page as a whole; use it when projecting the items of a page needs
        /// a single batched asynchronous call (for example resolving related data for every item of the page at once
        /// to avoid N+1 round trips).
        /// </summary>
        /// <param name="pageMapper">Maps the loaded page of <typeparamref name="TSource"/> items to the bound item type.</param>
        /// <returns>The configured collection, for fluent chaining.</returns>
        public InfiniteScrollCollection<TTarget> WithMapping(Func<IReadOnlyList<TSource>, Task<IReadOnlyList<TTarget>>> pageMapper)
        {
            ArgumentNullException.ThrowIfNull(pageMapper);

            var pageLoader = this.pageLoader;
            this.collection.UsePageLoader(async pagingInfo =>
            {
                var paginationSet = await pageLoader(pagingInfo);
                var sourceItems = paginationSet.Items as IReadOnlyList<TSource> ?? paginationSet.Items.ToList();
                var mappedItems = await pageMapper(sourceItems);
                return paginationSet.Map(_ => mappedItems);
            });

            return this.collection;
        }

        /// <summary>
        /// Sets the handler invoked before loading more items begins on the collection being configured, and
        /// returns this source so a mapping can still be chained.
        /// </summary>
        /// <param name="onBeforeLoadMore">The handler invoked before a load begins.</param>
        /// <returns>This source, for fluent chaining.</returns>
        public InfiniteScrollSource<TSource, TTarget> OnBeforeLoadMore(Action onBeforeLoadMore)
        {
            this.collection.OnBeforeLoadMore(onBeforeLoadMore);
            return this;
        }

        /// <summary>
        /// Sets the handler invoked after loading more items finishes on the collection being configured, and
        /// returns this source so a mapping can still be chained.
        /// </summary>
        /// <param name="onAfterLoadMore">The handler invoked after a load finishes.</param>
        /// <returns>This source, for fluent chaining.</returns>
        public InfiniteScrollSource<TSource, TTarget> OnAfterLoadMore(Action onAfterLoadMore)
        {
            this.collection.OnAfterLoadMore(onAfterLoadMore);
            return this;
        }

        /// <summary>
        /// Sets the handler invoked when a load operation throws on the collection being configured, and returns
        /// this source so a mapping can still be chained.
        /// </summary>
        /// <param name="onError">The handler invoked with the exception when a load fails.</param>
        /// <returns>This source, for fluent chaining.</returns>
        public InfiniteScrollSource<TSource, TTarget> OnError(Action<Exception> onError)
        {
            this.collection.OnError(onError);
            return this;
        }
    }
}