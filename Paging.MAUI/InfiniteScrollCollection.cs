using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Paging.MAUI
{

    /// <summary>
    /// A collection supporting incremental data loading for infinite scrolling scenarios.
    /// </summary>
    /// <typeparam name="T">The type of items contained in the collection.</typeparam>
    public class InfiniteScrollCollection<T> : ObservableCollection<T>, IInfiniteScrollLoader, IInfiniteScrollLoading
    {
        private bool isLoadingMore;

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
                    throw new InvalidOperationException("OnLoadMore must be set before calling LoadMoreAsync.");
                }

                var result = await loadMoreTask();
                if (result != null)
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
}