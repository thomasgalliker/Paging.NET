using FluentAssertions;
using Paging.MAUI.Tests.TestData;
using Xunit;

namespace Paging.MAUI.Tests
{
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
    }
}