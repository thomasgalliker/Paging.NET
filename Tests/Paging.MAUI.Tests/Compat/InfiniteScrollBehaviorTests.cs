namespace Paging.MAUI.Tests.Compat
{
    using InfiniteScrollBehavior = Paging.MAUI.Compat.InfiniteScrollBehavior;

    public class InfiniteScrollBehaviorTests
    {
        public InfiniteScrollBehaviorTests()
        {
            MauiMocks.Init();
        }

        [Fact]
        public async Task ShouldHandleOnListViewItemAppearing_OnCanLoadMoreFalse()
        {
            // Arrange
            var infiniteScrollBehavior = new InfiniteScrollBehavior();
            var listView = new ListView
            {
                BindingContext = new object()
            };

            const int count = 10;
            var items = Cars.CreateCars(count).ToList();
            var infiniteScrollCollection = new InfiniteScrollCollection<Car>(items);
            infiniteScrollCollection.OnCanLoadMore = () => false;

            listView.ItemsSource = infiniteScrollBehavior.ItemsSource = infiniteScrollCollection;
            listView.Behaviors.Add(infiniteScrollBehavior);

            // Act
            await infiniteScrollBehavior.OnListViewItemAppearingAsync(items.Last());

            // Assert
            infiniteScrollBehavior.AssociatedObject.Should().Be(listView);
        }

        [Fact]
        public async Task ShouldHandleOnListViewItemAppearing_OnCanLoadMoreTrue()
        {
            // Arrange
            var infiniteScrollBehavior = new InfiniteScrollBehavior();
            var listView = new ListView
            {
                BindingContext = new object()
            };

            const int initialCount = 10;
            var initialItems = Cars.CreateCars(initialCount).ToList();
            var infiniteScrollCollection = new InfiniteScrollCollection<Car>(initialItems);

            const int loadMoreCount = 5;
            infiniteScrollCollection.OnCanLoadMore = () => true;
            infiniteScrollCollection.OnLoadMore = () =>
            {
                var nextItems = Cars.CreateCars(5);
                return Task.FromResult(nextItems);
            };

            listView.ItemsSource = infiniteScrollBehavior.ItemsSource = infiniteScrollCollection;
            listView.Behaviors.Add(infiniteScrollBehavior);

            // Act
            await infiniteScrollBehavior.OnListViewItemAppearingAsync(initialItems.Last());

            // Assert
            infiniteScrollCollection.Should().HaveCount(initialCount + loadMoreCount);
        }

        [Fact]
        public async Task ShouldRecover_WhenLoadMoreThrows()
        {
            // Arrange
            var infiniteScrollBehavior = new InfiniteScrollBehavior();
            var listView = new ListView
            {
                BindingContext = new object()
            };

            const int initialCount = 10;
            var initialItems = Cars.CreateCars(initialCount).ToList();
            var infiniteScrollCollection = new InfiniteScrollCollection<Car>(initialItems);

            var throwOnLoadMore = true;
            infiniteScrollCollection.OnCanLoadMore = () => true;
            infiniteScrollCollection.OnLoadMore = () =>
            {
                if (throwOnLoadMore)
                {
                    throw new InvalidOperationException("Load operation failed");
                }

                return Task.FromResult(Cars.CreateCars(5));
            };

            listView.ItemsSource = infiniteScrollBehavior.ItemsSource = infiniteScrollCollection;
            listView.Behaviors.Add(infiniteScrollBehavior);

            // Act
            var exception = await Record.ExceptionAsync(() => infiniteScrollBehavior.OnListViewItemAppearingAsync(initialItems.Last()));

            throwOnLoadMore = false;
            await infiniteScrollBehavior.OnListViewItemAppearingAsync(initialItems.Last());

            // Assert
            exception.Should().BeOfType<InvalidOperationException>();
            infiniteScrollBehavior.IsLoadingMore.Should().BeFalse();
            infiniteScrollCollection.Should().HaveCount(initialCount + 5);
        }
    }
}
