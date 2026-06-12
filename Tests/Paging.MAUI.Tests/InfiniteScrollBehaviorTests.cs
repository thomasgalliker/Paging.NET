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
            var items = Cars.CreateCars(count).ToList();
            var infiniteScrollCollection = new InfiniteScrollCollection<Car>(items);
            infiniteScrollCollection.OnCanLoadMore = () => false;

            collectionView.ItemsSource = infiniteScrollBehavior.ItemsSource = infiniteScrollCollection;
            collectionView.Behaviors.Add(infiniteScrollBehavior);

            // Act
            await infiniteScrollBehavior.OnThresholdReachedAsync();

            // Assert
            infiniteScrollCollection.Should().HaveCount(count);
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

            var infiniteScrollCollection = new InfiniteScrollCollection<Car>(Cars.CreateCars(10));
            infiniteScrollCollection.OnCanLoadMore = () => true;
            infiniteScrollCollection.OnLoadMore = async () =>
            {
                loadMoreCount++;
                await loadMoreCompletion.Task;
                return Cars.CreateCars(5);
            };

            collectionView.ItemsSource = infiniteScrollBehavior.ItemsSource = infiniteScrollCollection;
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
            infiniteScrollCollection.Should().HaveCount(10 + 5);
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
            var infiniteScrollCollection = new InfiniteScrollCollection<Car>();
            infiniteScrollCollection.OnCanLoadMore = () => true;
            infiniteScrollCollection.OnLoadMore = () =>
            {
                if (throwOnLoadMore)
                {
                    throw new InvalidOperationException("Load operation failed");
                }

                return Task.FromResult(Cars.CreateCars(5));
            };

            collectionView.ItemsSource = infiniteScrollBehavior.ItemsSource = infiniteScrollCollection;
            collectionView.Behaviors.Add(infiniteScrollBehavior);

            // Act
            var exception = await Record.ExceptionAsync(() => infiniteScrollBehavior.OnThresholdReachedAsync());

            throwOnLoadMore = false;
            await infiniteScrollBehavior.OnThresholdReachedAsync();

            // Assert
            exception.Should().BeOfType<InvalidOperationException>();
            infiniteScrollBehavior.IsLoadingMore.Should().BeFalse();
            infiniteScrollCollection.Should().HaveCount(5);
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

            var infiniteScrollCollection = new InfiniteScrollCollection<Car>();
            infiniteScrollCollection.OnCanLoadMore = () => true;
            infiniteScrollCollection.OnLoadMore = async () =>
            {
                loadMoreCount++;
                await loadMoreCompletion.Task;
                return Cars.CreateCars(5);
            };

            collectionView.ItemsSource = infiniteScrollBehavior.ItemsSource = infiniteScrollCollection;
            collectionView.Behaviors.Add(infiniteScrollBehavior);

            // Act
            // Simulate an initial load started by the viewmodel (not by scrolling)
            // while RemainingItemsThresholdReached fires during the load.
            var initialLoadTask = infiniteScrollCollection.LoadMoreAsync();
            var thresholdReachedTask = infiniteScrollBehavior.OnThresholdReachedAsync();

            loadMoreCompletion.SetResult();
            await Task.WhenAll(initialLoadTask, thresholdReachedTask);

            // Assert
            loadMoreCount.Should().Be(1);
            infiniteScrollCollection.Should().HaveCount(5);
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
            var initialItems = Cars.CreateCars(initialCount).ToList();
            var infiniteScrollCollection = new InfiniteScrollCollection<Car>(initialItems);

            const int loadMoreCount = 5;
            infiniteScrollCollection.OnCanLoadMore = () => true;
            infiniteScrollCollection.OnLoadMore = () =>
            {
                var nextItems = Cars.CreateCars(loadMoreCount);
                return Task.FromResult(nextItems);
            };

            collectionView.ItemsSource = infiniteScrollBehavior.ItemsSource = infiniteScrollCollection;
            collectionView.Behaviors.Add(infiniteScrollBehavior);

            // Act
            await infiniteScrollBehavior.OnThresholdReachedAsync();

            // Assert
            infiniteScrollCollection.Should().HaveCount(initialCount + loadMoreCount);
        }
    }
}
