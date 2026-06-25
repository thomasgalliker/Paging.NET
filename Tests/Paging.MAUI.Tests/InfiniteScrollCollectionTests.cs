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

        [Fact]
        public void Constructor_Throws_WhenPageLoaderIsNull()
        {
            // Act
            Action action = () => new InfiniteScrollCollection<Car, CarDto>(null!, MapToDto);

            // Assert
            action.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("pageLoader");
        }

        [Fact]
        public void Constructor_Throws_WhenItemSelectorIsNull()
        {
            // Act
            Action action = () => new InfiniteScrollCollection<Car, CarDto>(p => Task.FromResult(new PaginationSet<Car>()), null!);

            // Assert
            action.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("itemSelector");
        }

        [Fact]
        public async Task InitializeAsync_LoadsAndMapsFirstPage()
        {
            // Arrange
            const int totalCount = 65;
            var pagingInfo = new PagingInfo { ItemsPerPage = 30 };
            var collection = new InfiniteScrollCollection<Car, CarDto>(
                pageLoader: p => Task.FromResult(new PaginationSet<Car>(p, CreatePage(p, totalCount), totalCount, totalCount)),
                itemSelector: MapToDto,
                pagingInfo: pagingInfo);

            // Act
            await collection.InitializeAsync();

            // Assert
            collection.Should().HaveCount(30);
            collection.Should().AllBeOfType<CarDto>();
            collection.First().Id.Should().Be(0);
            collection.CanLoadMore.Should().BeTrue();
            pagingInfo.CurrentPage.Should().Be(2);
        }

        [Fact]
        public async Task LoadsAllPages_ThenStops()
        {
            // Arrange
            const int totalCount = 65; // 30 + 30 + 5 => 3 pages
            var loadCount = 0;
            var pagingInfo = new PagingInfo { ItemsPerPage = 30 };
            var collection = new InfiniteScrollCollection<Car, CarDto>(
                pageLoader: p =>
                {
                    loadCount++;
                    return Task.FromResult(new PaginationSet<Car>(p, CreatePage(p, totalCount), totalCount, totalCount));
                },
                itemSelector: MapToDto,
                pagingInfo: pagingInfo);

            // Act
            await collection.InitializeAsync(); // page 1
            await collection.LoadMoreAsync();   // page 2
            await collection.LoadMoreAsync();   // page 3 (last)

            // Assert
            collection.Should().HaveCount(totalCount);
            collection.Select(c => c.Id).Should().BeEquivalentTo(Enumerable.Range(0, totalCount));
            collection.CanLoadMore.Should().BeFalse();
            loadCount.Should().Be(3);
            pagingInfo.CurrentPage.Should().Be(3); // stays on the last loaded page
        }

        [Fact]
        public async Task RefreshAsync_ClearsAndReloadsFromFirstPage()
        {
            // Arrange
            const int totalCount = 65;
            var pagingInfo = new PagingInfo { ItemsPerPage = 30 };
            var collection = new InfiniteScrollCollection<Car, CarDto>(
                pageLoader: p => Task.FromResult(new PaginationSet<Car>(p, CreatePage(p, totalCount), totalCount, totalCount)),
                itemSelector: MapToDto,
                pagingInfo: pagingInfo);

            await collection.InitializeAsync(); // page 1
            await collection.LoadMoreAsync();   // page 2 => 60 items, CurrentPage advanced to 3

            // Act
            await collection.RefreshAsync();

            // Assert
            collection.Should().HaveCount(30);     // back to the first page only
            collection.First().Id.Should().Be(0);
            pagingInfo.CurrentPage.Should().Be(2); // first page reloaded, advanced once
            collection.CanLoadMore.Should().BeTrue();
        }

        [Fact]
        public async Task RefreshAsync_DoesNothing_WhileLoading()
        {
            // Arrange
            const int totalCount = 65;
            var gate = new TaskCompletionSource<bool>();
            var loadCount = 0;
            var pagingInfo = new PagingInfo { ItemsPerPage = 30 };
            var collection = new InfiniteScrollCollection<Car, CarDto>(
                pageLoader: async p =>
                {
                    loadCount++;
                    if (loadCount == 1)
                    {
                        await gate.Task; // hold the first load open
                    }

                    return new PaginationSet<Car>(p, CreatePage(p, totalCount), totalCount, totalCount);
                },
                itemSelector: MapToDto,
                pagingInfo: pagingInfo);

            var initTask = collection.InitializeAsync();

            // Act
            collection.IsLoadingMore.Should().BeTrue();
            await collection.RefreshAsync(); // must no-op while the first load is in flight

            // Assert
            loadCount.Should().Be(1);
            collection.Should().BeEmpty();

            // Release the gate and let the initial load finish
            gate.SetResult(true);
            await initTask;
            collection.Should().HaveCount(30);
            loadCount.Should().Be(1);
        }

        [Fact]
        public async Task SingleGeneric_WithoutProjection_AppendsItemsAsIs()
        {
            // Arrange
            const int totalCount = 65;
            var pagingInfo = new PagingInfo { ItemsPerPage = 30 };
            var collection = new InfiniteScrollCollection<Car>(
                pageLoader: p => Task.FromResult(new PaginationSet<Car>(p, CreatePage(p, totalCount), totalCount, totalCount)),
                pagingInfo: pagingInfo);

            // Act
            await collection.InitializeAsync();

            // Assert
            collection.Should().HaveCount(30);
            collection.Should().AllBeOfType<Car>();
            collection.First().Id.Should().Be(0);
            collection.PagingInfo.Should().BeSameAs(pagingInfo);
            collection.CanLoadMore.Should().BeTrue();
        }

        [Fact]
        public async Task SingleGeneric_RefreshAsync_ReloadsFromFirstPage()
        {
            // Arrange
            const int totalCount = 65;
            var pagingInfo = new PagingInfo { ItemsPerPage = 30 };
            var collection = new InfiniteScrollCollection<Car>(
                pageLoader: p => Task.FromResult(new PaginationSet<Car>(p, CreatePage(p, totalCount), totalCount, totalCount)),
                pagingInfo: pagingInfo);

            await collection.InitializeAsync(); // page 1
            await collection.LoadMoreAsync();   // page 2 => 60 items

            // Act
            await collection.RefreshAsync();

            // Assert
            collection.Should().HaveCount(30);
            collection.First().Id.Should().Be(0);
            pagingInfo.CurrentPage.Should().Be(2);
        }

        [Fact]
        public void SingleGeneric_Throws_WhenPageLoaderIsNull()
        {
            // Act (cast disambiguates from the IEnumerable<T> constructor)
            Action action = () => new InfiniteScrollCollection<Car>((Func<PagingInfo, Task<PaginationSet<Car>>>)null!);

            // Assert
            action.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("pageLoader");
        }

        [Fact]
        public async Task LoadNextPage_WhenLastPageMappingThrows_StaysRetryable()
        {
            // Arrange: 35 items, 30 per page => 2 pages (30 + 5). Fail mapping the last page once.
            const int totalCount = 35;
            var pagingInfo = new PagingInfo { ItemsPerPage = 30 };
            var failLastPage = true;
            Exception? captured = null;
            var collection = new InfiniteScrollCollection<Car, CarDto>(
                pageLoader: p => Task.FromResult(new PaginationSet<Car>(p, CreatePage(p, totalCount), totalCount, totalCount)),
                itemSelector: car =>
                {
                    if (failLastPage && car.Id >= 30)
                    {
                        throw new InvalidOperationException("boom");
                    }

                    return MapToDto(car);
                },
                pagingInfo: pagingInfo)
            {
                OnError = ex => captured = ex,
            };

            await collection.InitializeAsync(); // page 1 maps fine (ids 0..29)

            // Act: the last page fails while mapping
            await collection.LoadMoreAsync();

            // Assert: page 1 intact, paging state untouched, last page still retryable
            captured.Should().BeOfType<InvalidOperationException>();
            collection.Should().HaveCount(30);
            pagingInfo.CurrentPage.Should().Be(2);    // not advanced past the failed page
            collection.CanLoadMore.Should().BeTrue();  // regression guard: must not record the failed last set

            // Act: retry the last page, now succeeding
            failLastPage = false;
            await collection.LoadMoreAsync();

            // Assert: every item is present and scrolling stops
            collection.Should().HaveCount(totalCount);
            collection.Select(c => c.Id).Should().BeEquivalentTo(Enumerable.Range(0, totalCount));
            collection.CanLoadMore.Should().BeFalse();
        }

        private static Car[] CreatePage(PagingInfo pagingInfo, int totalCount)
        {
            var itemsPerPage = pagingInfo.ItemsPerPage ?? totalCount;
            var from = (pagingInfo.CurrentPage - pagingInfo.FirstPageIndex) * itemsPerPage;
            var count = Math.Max(0, Math.Min(itemsPerPage, totalCount - from));
            return Enumerable.Range(from, count)
                .Select(i => new Car { Id = i, Name = $"Car {i}" })
                .ToArray();
        }

        private static CarDto MapToDto(Car car)
        {
            return new CarDto { Id = car.Id, Name = car.Name };
        }
    }
}
