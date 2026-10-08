namespace Paging.MAUI.Tests
{
    public class InfiniteScrollBehaviorTests
    {
        public InfiniteScrollBehaviorTests()
        {
            MauiMocks.Init();
        }

        [Fact]
        public void ShouldApplyRemainingItemsThreshold_OnAttachedTo()
        {
            // Arrange
            var infiniteScrollBehavior = new InfiniteScrollBehavior
            {
                RemainingItemsThreshold = 10
            };

            var collectionView = new CollectionView
            {
                BindingContext = new object()
            };

            // Act
            collectionView.Behaviors.Add(infiniteScrollBehavior);

            // Assert
            infiniteScrollBehavior.AssociatedObject.Should().Be(collectionView);
            collectionView.RemainingItemsThreshold.Should().Be(10);
        }

        [Fact]
        public void ShouldApplyRemainingItemsThreshold_OnPropertyChanged()
        {
            // Arrange
            var infiniteScrollBehavior = new InfiniteScrollBehavior();
            var collectionView = new CollectionView
            {
                BindingContext = new object()
            };

            collectionView.Behaviors.Add(infiniteScrollBehavior);

            // Act
            infiniteScrollBehavior.RemainingItemsThreshold = 20;

            // Assert
            collectionView.RemainingItemsThreshold.Should().Be(20);
        }

        [Fact]
        public async Task ShouldHandleOnThresholdReached_OnCanLoadMoreFalse()
        {
            // Arrange
            var infiniteScrollBehavior = new InfiniteScrollBehavior();
            var collectionView = new CollectionView
            {
                BindingContext = new object()
            };

            const int count = 10;
            var loader = new FakeInfiniteScrollLoader { CanLoadMoreFunc = () => false };
            loader.AddRange(Cars.CreateCarViewModels(count));

            collectionView.ItemsSource = infiniteScrollBehavior.ItemsSource = loader;
            collectionView.Behaviors.Add(infiniteScrollBehavior);

            // Act
            await infiniteScrollBehavior.OnThresholdReachedAsync();

            // Assert
            loader.Should().HaveCount(count);
        }

        [Fact]
        public async Task ShouldLoadOnlyOnce_WhenThresholdReachedRepeatedly()
        {
            // Arrange
            var infiniteScrollBehavior = new InfiniteScrollBehavior();
            var collectionView = new CollectionView
            {
                BindingContext = new object()
            };

            var loadMoreCount = 0;
            var loadMoreCompletion = new TaskCompletionSource();

            var loader = new FakeInfiniteScrollLoader
            {
                CanLoadMoreFunc = () => true,
                OnLoadMore = async () =>
                {
                    loadMoreCount++;
                    await loadMoreCompletion.Task;
                    return Cars.CreateCarViewModels(5);
                },
            };
            loader.AddRange(Cars.CreateCarViewModels(10));

            collectionView.ItemsSource = infiniteScrollBehavior.ItemsSource = loader;
            collectionView.Behaviors.Add(infiniteScrollBehavior);

            // Act
            // Simulate RemainingItemsThresholdReached firing repeatedly
            // while the first load operation is still in progress.
            var thresholdReachedTasks = Enumerable.Range(0, 10)
                .Select(_ => infiniteScrollBehavior.OnThresholdReachedAsync())
                .ToArray();

            loadMoreCompletion.SetResult();
            await Task.WhenAll(thresholdReachedTasks);

            // Assert
            loadMoreCount.Should().Be(1);
            loader.Should().HaveCount(10 + 5);
        }

        [Fact]
        public async Task ShouldRecover_WhenLoadMoreThrows()
        {
            // Arrange
            var infiniteScrollBehavior = new InfiniteScrollBehavior();
            var collectionView = new CollectionView
            {
                BindingContext = new object()
            };

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

            collectionView.ItemsSource = infiniteScrollBehavior.ItemsSource = loader;
            collectionView.Behaviors.Add(infiniteScrollBehavior);

            // Act
            var exception = await Record.ExceptionAsync(() => infiniteScrollBehavior.OnThresholdReachedAsync());

            throwOnLoadMore = false;
            await infiniteScrollBehavior.OnThresholdReachedAsync();

            // Assert
            exception.Should().BeOfType<InvalidOperationException>();
            infiniteScrollBehavior.IsLoadingMore.Should().BeFalse();
            loader.Should().HaveCount(5);
        }

        [Fact]
        public async Task ShouldNotLoad_WhenLoaderDrivenLoadIsInProgress()
        {
            // Arrange
            var infiniteScrollBehavior = new InfiniteScrollBehavior();
            var collectionView = new CollectionView
            {
                BindingContext = new object()
            };

            var loadMoreCount = 0;
            var loadMoreCompletion = new TaskCompletionSource();

            var loader = new FakeInfiniteScrollLoader
            {
                CanLoadMoreFunc = () => true,
                OnLoadMore = async () =>
                {
                    loadMoreCount++;
                    await loadMoreCompletion.Task;
                    return Cars.CreateCarViewModels(5);
                },
            };

            collectionView.ItemsSource = infiniteScrollBehavior.ItemsSource = loader;
            collectionView.Behaviors.Add(infiniteScrollBehavior);

            // Act
            // Simulate an initial load started by the viewmodel (not by scrolling)
            // while RemainingItemsThresholdReached fires during the load.
            var initialLoadTask = loader.LoadMoreAsync();
            var thresholdReachedTask = infiniteScrollBehavior.OnThresholdReachedAsync();

            loadMoreCompletion.SetResult();
            await Task.WhenAll(initialLoadTask, thresholdReachedTask);

            // Assert
            loadMoreCount.Should().Be(1);
            loader.Should().HaveCount(5);
        }

        [Fact]
        public async Task ShouldHandleOnThresholdReached_OnCanLoadMoreTrue()
        {
            // Arrange
            var infiniteScrollBehavior = new InfiniteScrollBehavior();
            var collectionView = new CollectionView
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

            collectionView.ItemsSource = infiniteScrollBehavior.ItemsSource = loader;
            collectionView.Behaviors.Add(infiniteScrollBehavior);

            // Act
            await infiniteScrollBehavior.OnThresholdReachedAsync();

            // Assert
            loader.Should().HaveCount(initialCount + loadMoreCount);
        }
    }
}
