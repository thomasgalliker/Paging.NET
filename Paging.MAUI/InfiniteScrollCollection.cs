using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Paging.MAUI
{
    public class InfiniteScrollCollection<T> : ObservableCollection<T>, IInfiniteScrollLoader, IInfiniteScrollLoading
    {
        private bool isLoadingMore;

        public InfiniteScrollCollection()
        {
        }

        public InfiniteScrollCollection(IEnumerable<T> collection)
            : base(collection)
        {
        }

        public Action OnBeforeLoadMore { get; set; }

        public Action OnAfterLoadMore { get; set; }

        public Action<Exception> OnError { get; set; }

        public Func<bool> OnCanLoadMore { get; set; }

        public Func<Task<IEnumerable<T>>> OnLoadMore { get; set; }

        public virtual bool CanLoadMore => this.OnCanLoadMore?.Invoke() ?? false;

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

        public event EventHandler<LoadingMoreEventArgs> LoadingMore;

        public async Task LoadMoreAsync()
        {
            try
            {
                this.IsLoadingMore = true;
                this.OnBeforeLoadMore?.Invoke();

                var result = await this.OnLoadMore();
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

        public void AddRange(IEnumerable<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

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