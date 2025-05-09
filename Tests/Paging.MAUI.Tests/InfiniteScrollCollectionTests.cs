using System.Collections.Specialized;
using FluentAssertions;
using Paging.MAUI.Tests.TestData;
using Xunit;

namespace Paging.MAUI.Tests
{
    public class InfiniteScrollCollectionTests
    {
        [Fact]
        public void ShouldCreateInfiniteScrollCollection_Empty()
        {
            // Act
            var infiniteScrollCollection = new InfiniteScrollCollection<Car>();

            // Assert
            infiniteScrollCollection.Should().BeEmpty();
            infiniteScrollCollection.CanLoadMore.Should().BeFalse();
            infiniteScrollCollection.OnLoadMore.Should().BeNull();
            infiniteScrollCollection.IsLoadingMore.Should().BeFalse();
        }

        [Fact]
        public void ShouldCreateInfiniteScrollCollection_WithItems()
        {
            // Arrange
            const int itemsCount = 10;
            var items = Cars.CreateCars(itemsCount);

            // Act
            var infiniteScrollCollection = new InfiniteScrollCollection<Car>(items);

            // Assert
            infiniteScrollCollection.Should().HaveCount(itemsCount);
            infiniteScrollCollection.CanLoadMore.Should().BeFalse();
            infiniteScrollCollection.OnLoadMore.Should().BeNull();
            infiniteScrollCollection.IsLoadingMore.Should().BeFalse();
        }

        [Fact]
        public async Task LoadMoreAsync_DoesNotAddRangeIfEmpty()
        {
            // Arrange
            const int pageCount = 10;
            var items = Cars.CreateCars(pageCount);
            var infiniteScrollCollection = new InfiniteScrollCollection<Car>(items);

            infiniteScrollCollection.OnCanLoadMore = () => true;
            infiniteScrollCollection.OnLoadMore = () =>
            {
                var nextItems = Enumerable.Empty<Car>();
                return Task.FromResult(nextItems);
            };

            // Act
            await infiniteScrollCollection.LoadMoreAsync();

            // Assert
            infiniteScrollCollection.Should().HaveCount(pageCount);
            infiniteScrollCollection.CanLoadMore.Should().BeTrue();
            infiniteScrollCollection.OnLoadMore.Should().NotBeNull();
            infiniteScrollCollection.IsLoadingMore.Should().BeFalse();
        }

        [Fact]
        public async Task LoadMoreAsync_AddRangeToExistingItems()
        {
            // Arrange
            const int pageCount = 10;
            var items = Cars.CreateCars(pageCount);
            var infiniteScrollCollection = new InfiniteScrollCollection<Car>(items);

            var collectionChangedEventArgs = new List<NotifyCollectionChangedEventArgs>();
            infiniteScrollCollection.CollectionChanged += (_, args) => { collectionChangedEventArgs.Add(args); };

            infiniteScrollCollection.OnCanLoadMore = () => true;
            infiniteScrollCollection.OnLoadMore = () =>
            {
                var nextItems = Cars.CreateCars(pageCount);
                return Task.FromResult(nextItems);
            };

            // Act
            await infiniteScrollCollection.LoadMoreAsync();

            // Assert
            infiniteScrollCollection.Should().HaveCount(2 * pageCount);
            infiniteScrollCollection.CanLoadMore.Should().BeTrue();
            infiniteScrollCollection.OnLoadMore.Should().NotBeNull();
            infiniteScrollCollection.IsLoadingMore.Should().BeFalse();

            collectionChangedEventArgs.Should().HaveCount(1);
            collectionChangedEventArgs.ElementAt(0).Action.Should().Be(NotifyCollectionChangedAction.Add);
            collectionChangedEventArgs.ElementAt(0).OldStartingIndex.Should().Be(-1);
            collectionChangedEventArgs.ElementAt(0).NewStartingIndex.Should().Be(10);
        }
    }
}