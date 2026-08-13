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
            var loader = new FakeInfiniteScrollLoader { CanLoadMoreFunc = () => false };
            loader.AddRange(Cars.CreateCarViewModels(count));

            listView.ItemsSource = infiniteScrollBehavior.ItemsSource = loader;
            listView.Behaviors.Add(infiniteScrollBehavior);

            // Act
            await infiniteScrollBehavior.OnListViewItemAppearingAsync(loader.Last());

            // Assert
            infiniteScrollBehavior.AssociatedObject.Should().Be(listView);
            loader.Should().HaveCount(count);
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
            const int loadMoreCount = 5;
            var loader = new FakeInfiniteScrollLoader
            {
                CanLoadMoreFunc = () => true,
                OnLoadMore = () => Task.FromResult<IEnumerable<CarViewModel>>(Cars.CreateCarViewModels(loadMoreCount)),
            };
            loader.AddRange(Cars.CreateCarViewModels(initialCount));

            listView.ItemsSource = infiniteScrollBehavior.ItemsSource = loader;
            listView.Behaviors.Add(infiniteScrollBehavior);

            // Act
            await infiniteScrollBehavior.OnListViewItemAppearingAsync(loader.Last());

            // Assert
            loader.Should().HaveCount(initialCount + loadMoreCount);
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
            var throwOnLoadMore = true;
            var loader = new FakeInfiniteScrollLoader
            {
                CanLoadMoreFunc = () => true,
                OnLoadMore = () =>
                {
                    if (throwOnLoadMore)
                    {
                        throw new InvalidOperationException("Load operation failed");
                    }

                    return Task.FromResult<IEnumerable<CarViewModel>>(Cars.CreateCarViewModels(5));
                },
            };
            loader.AddRange(Cars.CreateCarViewModels(initialCount));
            var lastItem = loader.Last();

            listView.ItemsSource = infiniteScrollBehavior.ItemsSource = loader;
            listView.Behaviors.Add(infiniteScrollBehavior);

            // Act
            var exception = await Record.ExceptionAsync(() => infiniteScrollBehavior.OnListViewItemAppearingAsync(lastItem));

            throwOnLoadMore = false;
            await infiniteScrollBehavior.OnListViewItemAppearingAsync(lastItem);

            // Assert
            exception.Should().BeOfType<InvalidOperationException>();
            infiniteScrollBehavior.IsLoadingMore.Should().BeFalse();
            loader.Should().HaveCount(initialCount + 5);
        }
    }
}
