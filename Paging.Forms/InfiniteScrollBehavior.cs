using System;
using System.Collections;
using Xamarin.Forms;

namespace Paging.Forms
{
    public class InfiniteScrollBehavior : Behavior<ListView>
    {
        private bool isLoadingMoreFromScroll;
        private bool isLoadingMoreFromLoader;
        private ListView associatedListView;

        public static readonly BindableProperty IsLoadingMoreProperty =
            BindableProperty.Create(
                nameof(IsLoadingMore),
                typeof(bool),
                typeof(InfiniteScrollBehavior),
                default(bool),
                BindingMode.OneWayToSource);

        public static readonly BindableProperty ItemsSourceProperty =
            BindableProperty.Create(
                nameof(ItemsSource),
                typeof(IEnumerable),
                typeof(InfiniteScrollBehavior),
                default(IEnumerable),
                BindingMode.OneWay,
                propertyChanged: OnItemsSourceChanged);

        public bool IsLoadingMore
        {
            get { return (bool)this.GetValue(IsLoadingMoreProperty); }
            private set { this.SetValue(IsLoadingMoreProperty, value); }
        }

        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)this.GetValue(ItemsSourceProperty); }
            set { this.SetValue(ItemsSourceProperty, value); }
        }

        protected override void OnAttachedTo(ListView bindable)
        {
            base.OnAttachedTo(bindable);

            this.associatedListView = bindable;

            //this.SetBinding(ItemsSourceProperty, new Binding(ListView.ItemsSourceProperty.PropertyName, source: this.associatedListView));

            bindable.BindingContextChanged += this.OnListViewBindingContextChanged;
            bindable.ItemAppearing += this.OnListViewItemAppearing;

            this.BindingContext = this.associatedListView.BindingContext;
        }

        protected override void OnDetachingFrom(ListView bindable)
        {
            this.RemoveBinding(ItemsSourceProperty);

            bindable.BindingContextChanged -= this.OnListViewBindingContextChanged;
            bindable.ItemAppearing -= this.OnListViewItemAppearing;

            base.OnDetachingFrom(bindable);
        }

        private void OnListViewBindingContextChanged(object sender, EventArgs e)
        {
            this.BindingContext = this.associatedListView.BindingContext;
        }

        private async void OnListViewItemAppearing(object sender, ItemVisibilityEventArgs e)
        {
            if (this.IsLoadingMore)
            {
                return;
            }

            if (this.ItemsSource is IInfiniteScrollLoader loader)
            {
                if (loader.CanLoadMore && this.ShouldLoadMore(e.Item))
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
            if (bindable is InfiniteScrollBehavior behavior)
            {
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