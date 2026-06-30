using System.ComponentModel;

namespace Paging.MAUI.Tests
{
    public class InfiniteScrollCollectionTests
    {
        [Fact]
        public void ShouldCreateInfiniteScrollCollection_Empty()
        {
            // Act
            var infiniteScrollCollection = new InfiniteScrollCollection<CarViewModel>();

            // Assert
            infiniteScrollCollection.Should().BeEmpty();
            infiniteScrollCollection.CanLoadMore.Should().BeFalse();
            infiniteScrollCollection.IsLoadingMore.Should().BeFalse();
        }

        [Fact]
        public void ShouldCreateInfiniteScrollCollection_WithItems()
        {
            // Arrange
            const int itemsCount = 10;
            var items = Cars.CreateCarViewModels(itemsCount);

            // Act
            var infiniteScrollCollection = new InfiniteScrollCollection<CarViewModel>(items);

            // Assert
            infiniteScrollCollection.Should().HaveCount(itemsCount);
            infiniteScrollCollection.CanLoadMore.Should().BeFalse();
            infiniteScrollCollection.IsLoadingMore.Should().BeFalse();
        }

        [Fact]
        public async Task LoadMoreAsync_AppendsEachPage_AsSingleCollectionChanged()
        {
            // Arrange: 20 items over 10/page => 2 pages.
            const int pageSize = 10;
            const int totalCount = 20;
            var pagingInfo = new PagingInfo { ItemsPerPage = pageSize };
            var collection = new InfiniteScrollCollection<CarViewModel>(pagingInfo)
                .WithPageLoader(p => Task.FromResult(new PaginationSet<CarDto>(p, CreatePage(p, totalCount), totalCount, totalCount)))
                .WithMapping(MapToViewModel);

            await collection.InitializeAsync(); // page 1: 10 items

            var collectionChangedEventArgs = new List<NotifyCollectionChangedEventArgs>();
            collection.CollectionChanged += (_, args) => collectionChangedEventArgs.Add(args);

            // Act
            await collection.LoadMoreAsync(); // page 2: 10 more

            // Assert: the page is appended as a single batched Add at the correct index.
            collection.Should().HaveCount(2 * pageSize);
            collection.CanLoadMore.Should().BeFalse();
            collection.IsLoadingMore.Should().BeFalse();

            collectionChangedEventArgs.Should().HaveCount(1);
            collectionChangedEventArgs.ElementAt(0).Action.Should().Be(NotifyCollectionChangedAction.Add);
            collectionChangedEventArgs.ElementAt(0).OldStartingIndex.Should().Be(-1);
            collectionChangedEventArgs.ElementAt(0).NewStartingIndex.Should().Be(10);
        }

        [Fact]
        public async Task WithoutMapping_BindsLoadedTypeDirectly()
        {
            // Arrange: the loaded type already is the bound type, so no mapping is configured. The page loader alone
            // fully configures the collection (the non-generic WithPageLoader overload) and the CarDtos are bound directly.
            const int totalCount = 65;
            var pagingInfo = new PagingInfo { ItemsPerPage = 30 };
            var collection = new InfiniteScrollCollection<CarDto>(pagingInfo)
                .WithPageLoader(p => Task.FromResult(new PaginationSet<CarDto>(p, CreatePage(p, totalCount), totalCount, totalCount)));

            // Act
            await collection.InitializeAsync();

            // Assert
            collection.Should().HaveCount(30);
            collection.Should().AllBeOfType<CarDto>();
            collection.First().Id.Should().Be(0);
            collection.PagingInfo.Should().BeSameAs(pagingInfo);
            collection.CanLoadMore.Should().BeTrue();
        }

        [Fact]
        public async Task WithMapping_ProjectsEachItem()
        {
            // Arrange: TSource (CarDto) inferred from the loader; the mapping needs no type argument.
            const int totalCount = 65;
            var pagingInfo = new PagingInfo { ItemsPerPage = 30 };
            var collection = new InfiniteScrollCollection<CarViewModel>(pagingInfo)
                .WithPageLoader(p => Task.FromResult(new PaginationSet<CarDto>(p, CreatePage(p, totalCount), totalCount, totalCount)))
                .WithMapping(MapToViewModel);

            // Act
            await collection.InitializeAsync();

            // Assert
            collection.Should().HaveCount(30);
            collection.Should().AllBeOfType<CarViewModel>();
            collection.First().Id.Should().Be(0);
            collection.CanLoadMore.Should().BeTrue();
            pagingInfo.CurrentPage.Should().Be(2);
        }

        [Fact]
        public async Task WithMapping_LastPaginationSetItems_AreTheBoundInstances()
        {
            // Arrange
            const int totalCount = 30;
            var pagingInfo = new PagingInfo { ItemsPerPage = 30 };
            var collection = new InfiniteScrollCollection<CarViewModel>(pagingInfo)
                .WithPageLoader(p => Task.FromResult(new PaginationSet<CarDto>(p, CreatePage(p, totalCount), totalCount, totalCount)))
                .WithMapping(MapToViewModel);

            // Act
            await collection.InitializeAsync();

            // Assert: the projection is materialized once, so LastPaginationSet.Items are the very instances
            // bound into the collection and re-enumerating does not re-run the mapping into duplicate instances.
            collection.LastPaginationSet!.Items.First().Should().BeSameAs(collection.First());
            collection.LastPaginationSet.Items.First().Should().BeSameAs(collection.LastPaginationSet.Items.First());
        }

        [Fact]
        public async Task WithMapping_PerPage_ProjectsEachPage()
        {
            // Arrange
            const int totalCount = 65;
            var pagingInfo = new PagingInfo { ItemsPerPage = 30 };
            var collection = new InfiniteScrollCollection<CarViewModel>(pagingInfo)
                .WithPageLoader(p => Task.FromResult(new PaginationSet<CarDto>(p, CreatePage(p, totalCount), totalCount, totalCount)))
                .WithMapping(MapPageToViewModel);

            // Act
            await collection.InitializeAsync();

            // Assert
            collection.Should().HaveCount(30);
            collection.Should().AllBeOfType<CarViewModel>();
            collection.First().Id.Should().Be(0);
            collection.CanLoadMore.Should().BeTrue();
            pagingInfo.CurrentPage.Should().Be(2);
        }

        [Fact]
        public async Task WithMapping_PerPage_RunsOncePerPage_ThenStops()
        {
            // Arrange: 65 items over 30/page => 3 pages. The page mapper must run once per page, not per item.
            const int totalCount = 65;
            var mapCount = 0;
            var pagingInfo = new PagingInfo { ItemsPerPage = 30 };
            var collection = new InfiniteScrollCollection<CarViewModel>(pagingInfo)
                .WithPageLoader(p => Task.FromResult(new PaginationSet<CarDto>(p, CreatePage(p, totalCount), totalCount, totalCount)))
                .WithMapping(dtos =>
                {
                    mapCount++;
                    return MapPageToViewModel(dtos);
                });

            // Act
            await collection.InitializeAsync();
            await collection.LoadMoreAsync();
            await collection.LoadMoreAsync();

            // Assert
            collection.Should().HaveCount(totalCount);
            collection.Select(c => c.Id).Should().BeEquivalentTo(Enumerable.Range(0, totalCount));
            collection.CanLoadMore.Should().BeFalse();
            mapCount.Should().Be(3);
        }

        [Fact]
        public async Task WithMapping_PerPage_WhenMapperThrows_StaysRetryable()
        {
            // Arrange: 35 items / 30 per page => 2 pages. Fail mapping the last page once.
            const int totalCount = 35;
            var pagingInfo = new PagingInfo { ItemsPerPage = 30 };
            var failLastPage = true;
            Exception? captured = null;
            var collection = new InfiniteScrollCollection<CarViewModel>(pagingInfo)
                .WithPageLoader(p => Task.FromResult(new PaginationSet<CarDto>(p, CreatePage(p, totalCount), totalCount, totalCount)))
                .WithMapping(dtos =>
                {
                    if (failLastPage && dtos.Any(c => c.Id >= 30))
                    {
                        throw new InvalidOperationException("boom");
                    }

                    return MapPageToViewModel(dtos);
                })
                .OnError(ex => captured = ex);

            await collection.InitializeAsync(); // page 1 maps fine (ids 0..29)

            // Act: the last page fails while mapping
            await collection.LoadMoreAsync();

            // Assert: page 1 intact, paging state and last-page metadata untouched, retry still possible
            captured.Should().BeOfType<InvalidOperationException>();
            collection.Should().HaveCount(30);
            pagingInfo.CurrentPage.Should().Be(2);
            collection.CanLoadMore.Should().BeTrue();
            collection.LastPaginationSet!.CurrentPage.Should().Be(1); // still the first page's set

            // Act: retry the last page, now succeeding
            failLastPage = false;
            await collection.LoadMoreAsync();

            // Assert
            collection.Should().HaveCount(totalCount);
            collection.Select(c => c.Id).Should().BeEquivalentTo(Enumerable.Range(0, totalCount));
            collection.CanLoadMore.Should().BeFalse();
        }

        [Fact]
        public async Task RefreshAsync_ReloadsFromFirstPage()
        {
            // Arrange
            const int totalCount = 65;
            var pagingInfo = new PagingInfo { ItemsPerPage = 30 };
            var collection = new InfiniteScrollCollection<CarViewModel>(pagingInfo)
                .WithPageLoader(p => Task.FromResult(new PaginationSet<CarDto>(p, CreatePage(p, totalCount), totalCount, totalCount)))
                .WithMapping(MapToViewModel);

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
        public async Task RefreshAsync_DoesNothing_WhileLoading()
        {
            // Arrange
            const int totalCount = 65;
            var gate = new TaskCompletionSource<bool>();
            var loadCount = 0;
            var pagingInfo = new PagingInfo { ItemsPerPage = 30 };
            var collection = new InfiniteScrollCollection<CarViewModel>(pagingInfo)
                .WithPageLoader(async p =>
                {
                    loadCount++;
                    if (loadCount == 1)
                    {
                        await gate.Task; // hold the first load open
                    }

                    return new PaginationSet<CarDto>(p, CreatePage(p, totalCount), totalCount, totalCount);
                })
                .WithMapping(MapToViewModel);

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
        public void WithPageLoader_Throws_WhenNull()
        {
            // Act
            Action action = () => new InfiniteScrollCollection<CarViewModel>(new PagingInfo()).WithPageLoader<CarDto>(null!);

            // Assert
            action.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("pageLoader");
        }

        [Fact]
        public void WithPageLoader_Identity_Throws_WhenNull()
        {
            // Act: with no type argument the non-generic (identity) overload is selected.
            Action action = () => new InfiniteScrollCollection<CarDto>(new PagingInfo()).WithPageLoader(null!);

            // Assert
            action.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("pageLoader");
        }

        [Fact]
        public void WithMapping_PerItem_Throws_WhenNull()
        {
            // Act: the cast selects the per-item overload (both WithMapping overloads accept null).
            Action action = () => new InfiniteScrollCollection<CarViewModel>(new PagingInfo())
                .WithPageLoader(p => Task.FromResult(new PaginationSet<CarDto>()))
                .WithMapping((Func<CarDto, CarViewModel>)null!);

            // Assert
            action.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("itemSelector");
        }

        [Fact]
        public void WithMapping_PerPage_Throws_WhenNull()
        {
            // Act: the cast selects the per-page overload (both WithMapping overloads accept null).
            Action action = () => new InfiniteScrollCollection<CarViewModel>(new PagingInfo())
                .WithPageLoader(p => Task.FromResult(new PaginationSet<CarDto>()))
                .WithMapping((Func<IReadOnlyList<CarDto>, Task<IReadOnlyList<CarViewModel>>>)null!);

            // Assert
            action.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("pageMapper");
        }

        [Fact]
        public void LastPaginationSet_IsNull_BeforeFirstLoad()
        {
            // Arrange
            var collection = new InfiniteScrollCollection<CarViewModel>(new PagingInfo { ItemsPerPage = 30 })
                .WithPageLoader(p => Task.FromResult(new PaginationSet<CarDto>(p, CreatePage(p, 65), 65, 65)))
                .WithMapping(MapToViewModel);

            // Assert
            collection.LastPaginationSet.Should().BeNull();
        }

        [Fact]
        public async Task LastPaginationSet_IsPopulated_WithServerTotals_AfterLoad()
        {
            // Arrange: 42 total matching, 50 unfiltered.
            const int totalCount = 42;
            const int totalCountUnfiltered = 50;
            var pagingInfo = new PagingInfo { ItemsPerPage = 30 };
            var collection = new InfiniteScrollCollection<CarViewModel>(pagingInfo)
                .WithPageLoader(p => Task.FromResult(new PaginationSet<CarDto>(p, CreatePage(p, totalCount), totalCount, totalCountUnfiltered)))
                .WithMapping(MapToViewModel);

            // Act
            await collection.InitializeAsync();

            // Assert
            collection.LastPaginationSet.Should().NotBeNull();
            collection.LastPaginationSet!.TotalCount.Should().Be(totalCount);
            collection.LastPaginationSet.TotalCountUnfiltered.Should().Be(totalCountUnfiltered);
        }

        [Fact]
        public async Task LastPaginationSet_RaisesPropertyChanged_OnLoad()
        {
            // Arrange
            var pagingInfo = new PagingInfo { ItemsPerPage = 30 };
            var collection = new InfiniteScrollCollection<CarViewModel>(pagingInfo)
                .WithPageLoader(p => Task.FromResult(new PaginationSet<CarDto>(p, CreatePage(p, 65), 65, 65)))
                .WithMapping(MapToViewModel);

            var changed = new List<string?>();
            ((INotifyPropertyChanged)collection).PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            // Act
            await collection.InitializeAsync();

            // Assert
            changed.Should().Contain(nameof(InfiniteScrollCollection<CarViewModel>.LastPaginationSet));
        }

        [Fact]
        public async Task LastPaginationSet_IsResetToNull_DuringRefresh()
        {
            // Arrange: hold the reload of the refresh open so the reset is observable.
            const int totalCount = 65;
            var gate = new TaskCompletionSource<bool>();
            var loadCount = 0;
            var pagingInfo = new PagingInfo { ItemsPerPage = 30 };
            var collection = new InfiniteScrollCollection<CarViewModel>(pagingInfo)
                .WithPageLoader(async p =>
                {
                    loadCount++;
                    if (loadCount == 2)
                    {
                        await gate.Task; // hold the reload open
                    }

                    return new PaginationSet<CarDto>(p, CreatePage(p, totalCount), totalCount, totalCount);
                })
                .WithMapping(MapToViewModel);

            await collection.InitializeAsync();
            collection.LastPaginationSet.Should().NotBeNull();

            // Act: start a refresh; it resets state synchronously, then awaits the gated reload
            var refreshTask = collection.RefreshAsync();

            // Assert: reset observed while the reload is in flight
            collection.LastPaginationSet.Should().BeNull();

            // Release and finish
            gate.SetResult(true);
            await refreshTask;
            collection.LastPaginationSet.Should().NotBeNull();
        }

        [Fact]
        public async Task LastPaginationSet_NotSet_WhenFirstLoadFaults()
        {
            // Arrange
            Exception? captured = null;
            var collection = new InfiniteScrollCollection<CarViewModel>(new PagingInfo { ItemsPerPage = 30 })
                .WithPageLoader(p => Task.FromException<PaginationSet<CarDto>>(new InvalidOperationException("boom")))
                .WithMapping(MapToViewModel)
                .OnError(ex => captured = ex);

            // Act
            await collection.InitializeAsync();

            // Assert
            captured.Should().BeOfType<InvalidOperationException>();
            collection.LastPaginationSet.Should().BeNull();
        }

        [Fact]
        public async Task OnError_ReportsLoadFailures()
        {
            // Arrange: error handler set fluently after the mapping (on the collection).
            Exception? captured = null;
            var collection = new InfiniteScrollCollection<CarViewModel>(new PagingInfo { ItemsPerPage = 30 })
                .WithPageLoader(p => Task.FromException<PaginationSet<CarDto>>(new InvalidOperationException("boom")))
                .WithMapping(MapToViewModel)
                .OnError(ex => captured = ex);

            // Act
            await collection.InitializeAsync();

            // Assert
            captured.Should().BeOfType<InvalidOperationException>();
        }

        [Fact]
        public async Task OnError_OnSource_ReportsLoadFailures()
        {
            // Arrange: error handler set fluently mid-chain (on the source, before the mapping).
            Exception? captured = null;
            var collection = new InfiniteScrollCollection<CarViewModel>(new PagingInfo { ItemsPerPage = 30 })
                .WithPageLoader(p => Task.FromException<PaginationSet<CarDto>>(new InvalidOperationException("boom")))
                .OnError(ex => captured = ex)
                .WithMapping(MapToViewModel);

            // Act
            await collection.InitializeAsync();

            // Assert
            captured.Should().BeOfType<InvalidOperationException>();
        }

        [Fact]
        public void OnError_Throws_WhenNull()
        {
            // Act
            Action action = () => new InfiniteScrollCollection<CarViewModel>(new PagingInfo()).OnError(null!);

            // Assert
            action.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("onError");
        }

        private static CarDto[] CreatePage(PagingInfo pagingInfo, int totalCount)
        {
            var itemsPerPage = pagingInfo.ItemsPerPage ?? totalCount;
            var from = (pagingInfo.CurrentPage - pagingInfo.FirstPageIndex) * itemsPerPage;
            var count = Math.Max(0, Math.Min(itemsPerPage, totalCount - from));
            return Enumerable.Range(from, count)
                .Select(i => new CarDto { Id = i, Name = $"Car {i}" })
                .ToArray();
        }

        private static CarViewModel MapToViewModel(CarDto dto)
        {
            return new CarViewModel { Id = dto.Id, Name = dto.Name };
        }

        private static Task<IReadOnlyList<CarViewModel>> MapPageToViewModel(IReadOnlyList<CarDto> dtos)
        {
            return Task.FromResult<IReadOnlyList<CarViewModel>>(dtos.Select(MapToViewModel).ToArray());
        }
    }
}
