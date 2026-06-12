using System.Collections;
using Paging.MAUI.Internals;

namespace Paging.MAUI
{
    /// <summary>
    /// Infinite-scroll behavior for <see cref="CollectionView"/> and other <see cref="ItemsView"/>-derived controls.
    /// Uses the native <see cref="ItemsView.RemainingItemsThresholdReached"/> mechanism to load
    /// the next page from an <see cref="IInfiniteScrollLoader"/> items source.
    /// </summary>
    public class InfiniteScrollBehavior : BehaviorBase<ItemsView>
    {
        private bool isLoadingMoreFromScroll;
        private bool isLoadingMoreFromLoader;

        public static readonly BindableProperty IsLoadingMoreProperty =
            BindableProperty.Create(
                nameof(IsLoadingMore),
                typeof(bool),
                typeof(InfiniteScrollBehavior),
                false,
                BindingMode.OneWayToSource);

        public bool IsLoadingMore
        {
            get => (bool)this.GetValue(IsLoadingMoreProperty);
            private set => this.SetValue(IsLoadingMoreProperty, value);
        }

        public static readonly BindableProperty ItemsSourceProperty =
            BindableProperty.Create(
                nameof(ItemsSource),
                typeof(IEnumerable),
                typeof(InfiniteScrollBehavior),
                propertyChanged: OnItemsSourceChanged);

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)this.GetValue(ItemsSourceProperty);
            set => this.SetValue(ItemsSourceProperty, value);
        }

        public static readonly BindableProperty RemainingItemsThresholdProperty =
            BindableProperty.Create(
                nameof(RemainingItemsThreshold),
                typeof(int),
                typeof(InfiniteScrollBehavior),
                5,
                propertyChanged: OnRemainingItemsThresholdChanged);

        /// <summary>
        /// Number of items not yet scrolled to at which loading of the next page is triggered.
        /// The value is applied to the attached <see cref="ItemsView"/> and overwrites any
        /// <see cref="ItemsView.RemainingItemsThreshold"/> set directly on the view. Default: 5.
        /// </summary>
        public int RemainingItemsThreshold
        {
            get => (int)this.GetValue(RemainingItemsThresholdProperty);
            set => this.SetValue(RemainingItemsThresholdProperty, value);
        }

        protected override void OnAttachedTo(ItemsView bindable)
        {
            base.OnAttachedTo(bindable);
            bindable.RemainingItemsThreshold = this.RemainingItemsThreshold;
            bindable.RemainingItemsThresholdReached += this.OnRemainingItemsThresholdReached;
        }

        protected override void OnDetachingFrom(ItemsView bindable)
        {
            this.RemoveBinding(ItemsSourceProperty);
            bindable.RemainingItemsThresholdReached -= this.OnRemainingItemsThresholdReached;
            base.OnDetachingFrom(bindable);
        }

        private static void OnRemainingItemsThresholdChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is InfiniteScrollBehavior { AssociatedObject: ItemsView itemsView })
            {
                itemsView.RemainingItemsThreshold = (int)newValue;
            }
        }

        private async void OnRemainingItemsThresholdReached(object? sender, EventArgs e)
        {
            await this.OnThresholdReachedAsync();
        }

        internal async Task OnThresholdReachedAsync()
        {
            if (this.IsLoadingMore)
            {
                return;
            }

            if (this.ItemsSource is IInfiniteScrollLoader loader)
            {
                if (loader.CanLoadMore)
                {
                    this.UpdateIsLoadingMore(true, null);
                    try
                    {
                        await loader.LoadMoreAsync();
                    }
                    finally
                    {
                        this.UpdateIsLoadingMore(false, null);
                    }
                }
            }
        }

        private static void OnItemsSourceChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is not InfiniteScrollBehavior behavior)
            {
                return;
            }

            if (oldValue is IInfiniteScrollLoading oldLoading)
            {
                oldLoading.LoadingMore -= behavior.OnLoadingMore;
                behavior.UpdateIsLoadingMore(null, false);
            }

            if (newValue is IInfiniteScrollLoading newLoading)
            {
                newLoading.LoadingMore += behavior.OnLoadingMore;
                behavior.UpdateIsLoadingMore(null, newLoading.IsLoadingMore);
            }
        }

        private void OnLoadingMore(object? sender, LoadingMoreEventArgs e)
        {
            this.UpdateIsLoadingMore(null, e.IsLoadingMore);
        }

        private void UpdateIsLoadingMore(bool? fromScroll, bool? fromLoader)
        {
            this.isLoadingMoreFromScroll = fromScroll ?? this.isLoadingMoreFromScroll;
            this.isLoadingMoreFromLoader = fromLoader ?? this.isLoadingMoreFromLoader;

            this.IsLoadingMore = this.isLoadingMoreFromScroll || this.isLoadingMoreFromLoader;
        }
    }
}
