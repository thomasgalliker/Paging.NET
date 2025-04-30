using System.Collections;
using Paging.MAUI.Internals;

namespace Paging.MAUI
{
    public class InfiniteScrollBehavior : BehaviorBase<ListView>
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

        public static readonly BindableProperty ItemsSourceProperty =
            BindableProperty.Create(
                nameof(ItemsSource),
                typeof(IEnumerable),
                typeof(InfiniteScrollBehavior),
                propertyChanged: OnItemsSourceChanged);

        public bool IsLoadingMore
        {
            get => (bool)this.GetValue(IsLoadingMoreProperty);
            private set => this.SetValue(IsLoadingMoreProperty, value);
        }

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)this.GetValue(ItemsSourceProperty);
            set => this.SetValue(ItemsSourceProperty, value);
        }

        protected override void OnAttachedTo(ListView bindable)
        {
            base.OnAttachedTo(bindable);

            bindable.ItemAppearing += this.OnListViewItemAppearing;
        }

        protected override void OnDetachingFrom(ListView bindable)
        {
            this.RemoveBinding(ItemsSourceProperty);

            bindable.ItemAppearing -= this.OnListViewItemAppearing;

            base.OnDetachingFrom(bindable);
        }

        private async void OnListViewItemAppearing(object sender, ItemVisibilityEventArgs e)
        {
            await this.OnListViewItemAppearingAsync(e.Item);
        }

        internal async Task OnListViewItemAppearingAsync(object item)
        {
            if (this.IsLoadingMore)
            {
                return;
            }

            if (this.ItemsSource is IInfiniteScrollLoader loader)
            {
                if (loader.CanLoadMore && this.ShouldLoadMore(item))
                {
                    this.UpdateIsLoadingMore(true, null);
                    await loader.LoadMoreAsync();
                    this.UpdateIsLoadingMore(false, null);
                }
            }
        }

        private bool ShouldLoadMore(object item)
        {
            if (this.ItemsSource is IList list)
            {
                if (list.Count == 0)
                {
                    return true;
                }

                var lastItem = list[list.Count - 1];
                var isLastItem = lastItem == item;
                return isLastItem;
            }

            return false;
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

        private void OnLoadingMore(object sender, LoadingMoreEventArgs e)
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