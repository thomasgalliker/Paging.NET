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
