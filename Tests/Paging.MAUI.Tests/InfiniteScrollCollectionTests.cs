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
        public async Task WithMapping_PaginationSetItems_AreTheBoundInstances()
        {
            // Arrange
            const int totalCount = 30;
            var pagingInfo = new PagingInfo { ItemsPerPage = 30 };
            var collection = new InfiniteScrollCollection<CarViewModel>(pagingInfo)
                .WithPageLoader(p => Task.FromResult(new PaginationSet<CarDto>(p, CreatePage(p, totalCount), totalCount, totalCount)))
                .WithMapping(MapToViewModel);

            // Act
            await collection.InitializeAsync();

            // Assert: the projection is materialized once, so PaginationSet.Items are the very instances
            // bound into the collection and re-enumerating does not re-run the mapping into duplicate instances.
            collection.PaginationSet!.Items.First().Should().BeSameAs(collection.First());
            collection.PaginationSet.Items.First().Should().BeSameAs(collection.PaginationSet.Items.First());
        }

        [Fact]
        public async Task WithMapping_PerPage_ProjectsEachPage()
        {
            // Arrange
            const int totalCount = 65;
            var pagingInfo = new PagingInfo { ItemsPerPage = 30 };
            var collection = new InfiniteScrollCollection<CarViewModel>(pagingInfo)
                .WithPageLoader(p => Task.FromResult(new PaginationSet<CarDto>(p, CreatePage(p, totalCount), totalCount, totalCount)))
                .WithMapping(MapDtosToViewModels);

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
                    return MapDtosToViewModels(dtos);
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

                    return MapDtosToViewModels(dtos);
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
            collection.PaginationSet!.CurrentPage.Should().Be(1); // still the first page's set

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
        public async Task RefreshAsync_QueuesBehindInFlightLoad_ThenReloads()
        {
            // Arrange: hold the first load open so a refresh issued during it must be queued, not dropped.
            const int totalCount = 30;
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
            collection.IsLoadingMore.Should().BeTrue();

            // Act: refresh while the first load is in flight => queued behind it, not dropped
            var refreshTask = collection.RefreshAsync();

            // Assert: queued, but not run yet (the in-flight load has not completed)
            refreshTask.IsCompleted.Should().BeFalse();
            loadCount.Should().Be(1);

            // Release the gate; the queued refresh then reloads from the first page
            gate.SetResult(true);
            await Task.WhenAll(initTask, refreshTask);

            // Assert: the refresh ran exactly once after the in-flight load completed
            loadCount.Should().Be(2);
            collection.Should().HaveCount(30);
            collection.First().Id.Should().Be(0);
        }

        [Fact]
        public async Task RefreshAsync_CoalescesConcurrentRequests_IntoASingleReload()
        {
            // Arrange: hold the first load open and issue several refreshes during it.
            const int totalCount = 30;
            var gate = new TaskCompletionSource<bool>();
            var loadCount = 0;
            var pagingInfo = new PagingInfo { ItemsPerPage = 30 };
            var collection = new InfiniteScrollCollection<CarViewModel>(pagingInfo)
                .WithPageLoader(async p =>
                {
                    loadCount++;
                    if (loadCount == 1)
                    {
                        await gate.Task;
                    }

                    return new PaginationSet<CarDto>(p, CreatePage(p, totalCount), totalCount, totalCount);
                })
                .WithMapping(MapToViewModel);

            var initTask = collection.InitializeAsync();

            // Act: three refreshes while the first load is in flight
            var refresh1 = collection.RefreshAsync();
            var refresh2 = collection.RefreshAsync();
            var refresh3 = collection.RefreshAsync();
            loadCount.Should().Be(1);

            gate.SetResult(true);
            await Task.WhenAll(initTask, refresh1, refresh2, refresh3);

            // Assert: the three refreshes collapse into a single reload (1 initial load + 1 reload, not + 3).
            loadCount.Should().Be(2);
            collection.Should().HaveCount(30);
        }

        [Fact]
        public async Task RefreshAsync_RequestedDuringDrainReload_TriggersAnotherReload()
        {
            // Arrange: gate the drain's own reload so a refresh can be issued while that reload is in flight.
            // This exercises the second-level coalescing of the drain loop (an `if` instead of `while` would miss it).
            var gate = new TaskCompletionSource<bool>();
            var loadCount = 0;
            var pagingInfo = new PagingInfo { ItemsPerPage = 30 };
            var collection = new InfiniteScrollCollection<CarViewModel>(pagingInfo)
                .WithPageLoader(async p =>
                {
                    loadCount++;
                    if (loadCount == 2)
                    {
                        await gate.Task; // hold the first refresh's reload open
                    }

                    return new PaginationSet<CarDto>(p, CreatePage(p, 30), 30, 30);
                })
                .WithMapping(MapToViewModel);

            await collection.InitializeAsync(); // load 1

            // Act: start a refresh (drain reload = load 2, gated) and issue a second refresh while it is in flight
            var refresh1 = collection.RefreshAsync();
            loadCount.Should().Be(2);
            collection.IsLoadingMore.Should().BeTrue();

            var refresh2 = collection.RefreshAsync(); // requested during the drain's reload
            gate.SetResult(true);
            await Task.WhenAll(refresh1, refresh2);

            // Assert: the second refresh caused one more reload (load 3), proving the drain loops to honor it.
            loadCount.Should().Be(3);
            collection.Should().HaveCount(30);
        }

        [Fact]
        public async Task RefreshAsync_FaultingReloadWithoutOnError_SurfacesError_AndStaysRecoverable()
        {
            // Arrange: NO onError handler, so a faulting reload propagates to the awaiter. Fail the next reload once.
            var loadCount = 0;
            var failNextReload = false;
            var pagingInfo = new PagingInfo { ItemsPerPage = 30 };
            var collection = new InfiniteScrollCollection<CarViewModel>(pagingInfo)
                .WithPageLoader(p =>
                {
                    loadCount++;
                    if (failNextReload)
                    {
                        failNextReload = false;
                        return Task.FromException<PaginationSet<CarDto>>(new InvalidOperationException("reload boom"));
                    }

                    return Task.FromResult(new PaginationSet<CarDto>(p, CreatePage(p, 30), 30, 30));
                })
                .WithMapping(MapToViewModel);

            await collection.InitializeAsync(); // load 1 ok
            collection.Should().HaveCount(30);

            // Act: the next refresh's reload faults and (no onError) propagates to the awaiter
            failNextReload = true;
            Func<Task> faulting = () => collection.RefreshAsync();
            await faulting.Should().ThrowAsync<InvalidOperationException>();

            // Assert: the refresh cleared the collection, and the drain is not wedged
            collection.Should().BeEmpty();

            // A subsequent refresh reloads cleanly — the pending refresh was not silently consumed by the fault
            await collection.RefreshAsync();
            collection.Should().HaveCount(30);
            loadCount.Should().Be(3);
        }

        [Fact]
        public async Task RefreshAsync_IntoEmptyResult_ClearsAndPublishesEmptyMetadata()
        {
            // Arrange: first load returns 30 items; after toggling, a refresh returns an empty result
            // (e.g. a search that matches nothing) while the unfiltered total stays > 0.
            var empty = false;
            var pagingInfo = new PagingInfo { ItemsPerPage = 30 };
            var collection = new InfiniteScrollCollection<CarViewModel>(pagingInfo)
                .WithPageLoader(p => empty
                    ? Task.FromResult(new PaginationSet<CarDto>(p, Array.Empty<CarDto>(), 0, 50))
                    : Task.FromResult(new PaginationSet<CarDto>(p, CreatePage(p, 30), 30, 50)))
                .WithMapping(MapToViewModel);

            await collection.InitializeAsync();
            collection.Should().HaveCount(30);

            var collectionChanged = new List<NotifyCollectionChangedEventArgs>();
            collection.CollectionChanged += (_, e) => collectionChanged.Add(e);

            // Act: refresh into an empty result
            empty = true;
            await collection.RefreshAsync();

            // Assert: cleared, no spurious empty Add, and empty-state metadata published (filtered 0 of 50).
            collection.Should().BeEmpty();
            collectionChanged.Should().NotContain(e => e.Action == NotifyCollectionChangedAction.Add);
            collection.PaginationSet.Should().NotBeNull();
            collection.PaginationSet!.TotalCount.Should().Be(0);
            collection.PaginationSet.TotalCountUnfiltered.Should().Be(50);
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
        public void PaginationSet_IsNull_BeforeFirstLoad()
        {
            // Arrange
            var collection = new InfiniteScrollCollection<CarViewModel>(new PagingInfo { ItemsPerPage = 30 })
                .WithPageLoader(p => Task.FromResult(new PaginationSet<CarDto>(p, CreatePage(p, 65), 65, 65)))
                .WithMapping(MapToViewModel);

            // Assert
            collection.PaginationSet.Should().BeNull();
        }

        [Fact]
        public async Task PaginationSet_IsPopulated_WithServerTotals_AfterLoad()
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
            collection.PaginationSet.Should().NotBeNull();
            collection.PaginationSet!.TotalCount.Should().Be(totalCount);
            collection.PaginationSet.TotalCountUnfiltered.Should().Be(totalCountUnfiltered);
        }

        [Fact]
        public async Task PaginationSet_RaisesPropertyChanged_OnLoad()
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
            changed.Should().Contain(nameof(InfiniteScrollCollection<CarViewModel>.PaginationSet));
        }

        [Fact]
        public async Task PaginationSet_IsResetToNull_DuringRefresh()
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
            collection.PaginationSet.Should().NotBeNull();

            // Act: start a refresh; it resets state synchronously, then awaits the gated reload
            var refreshTask = collection.RefreshAsync();

            // Assert: reset observed while the reload is in flight
            collection.PaginationSet.Should().BeNull();

            // Release and finish
            gate.SetResult(true);
            await refreshTask;
            collection.PaginationSet.Should().NotBeNull();
        }

        [Fact]
        public async Task PaginationSet_NotSet_WhenFirstLoadFaults()
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
            collection.PaginationSet.Should().BeNull();
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

        [Fact]
        public async Task OnPaginationSetChanged_InvokedOnLoad_AndOnRefreshReset()
        {
            // Arrange: handler set fluently after the mapping (on the collection).
            var invocationCount = 0;
            var pagingInfo = new PagingInfo { ItemsPerPage = 30 };
            var collection = new InfiniteScrollCollection<CarViewModel>(pagingInfo)
                .WithPageLoader(p => Task.FromResult(new PaginationSet<CarDto>(p, CreatePage(p, 65), 65, 65)))
                .WithMapping(MapToViewModel)
                .OnPaginationSetChanged(() => invocationCount++);

            // Act: first load populates PaginationSet
            await collection.InitializeAsync();

            // Assert
            invocationCount.Should().Be(1);
            collection.PaginationSet.Should().NotBeNull();

            // Act: RefreshAsync resets PaginationSet to null, then reloads it => 2 more invocations
            await collection.RefreshAsync();

            // Assert
            invocationCount.Should().Be(3);
        }

        [Fact]
        public async Task OnPaginationSetChanged_OnSource_IsInvoked()
        {
            // Arrange: handler set fluently mid-chain (on the source, before the mapping).
            var invoked = false;
            var collection = new InfiniteScrollCollection<CarViewModel>(new PagingInfo { ItemsPerPage = 30 })
                .WithPageLoader(p => Task.FromResult(new PaginationSet<CarDto>(p, CreatePage(p, 30), 30, 30)))
                .OnPaginationSetChanged(() => invoked = true)
                .WithMapping(MapToViewModel);

            // Act
            await collection.InitializeAsync();

            // Assert
            invoked.Should().BeTrue();
        }

        [Fact]
        public void OnPaginationSetChanged_Throws_WhenNull()
        {
            // Act
            Action action = () => new InfiniteScrollCollection<CarViewModel>(new PagingInfo()).OnPaginationSetChanged(null!);

            // Assert
            action.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("onPaginationSetChanged");
        }

        [Fact]
        public async Task LoadMoreAsync_WhenOnErrorHandlerItselfThrows_DoesNotPropagate()
        {
            // Arrange: the load fails and the error handler also throws.
            var collection = new InfiniteScrollCollection<CarViewModel>(new PagingInfo { ItemsPerPage = 30 })
                .WithPageLoader(p => Task.FromException<PaginationSet<CarDto>>(new InvalidOperationException("load failed")))
                .WithMapping(MapToViewModel)
                .OnError(_ => throw new InvalidOperationException("handler failed"));

            // Act
            Func<Task> act = () => collection.InitializeAsync();

            // Assert: a faulting handler must not escape (it would crash async-void scroll loads); state still resets.
            await act.Should().NotThrowAsync();
            collection.IsLoadingMore.Should().BeFalse();
        }

        [Fact]
        public async Task LoadMoreAsync_DoesNotReenter_WhileLoading()
        {
            // Arrange: hold the first load open, then issue a second concurrent LoadMoreAsync.
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
                        await gate.Task;
                    }

                    return new PaginationSet<CarDto>(p, CreatePage(p, totalCount), totalCount, totalCount);
                })
                .WithMapping(MapToViewModel);

            var first = collection.InitializeAsync();
            collection.IsLoadingMore.Should().BeTrue();

            // Act: a second load while the first is in flight must be ignored (no double-advance, no duplicate page).
            await collection.LoadMoreAsync();

            // Assert: the overlapping call was a no-op
            loadCount.Should().Be(1);

            // Release the first load and verify only the one page was applied
            gate.SetResult(true);
            await first;
            collection.Should().HaveCount(30);
            pagingInfo.CurrentPage.Should().Be(2);
        }

        [Fact]
        public void CanLoadMore_IsFalse_BeforeFirstLoad()
        {
            // Arrange: a page loader is configured but the first page has not been loaded yet.
            var collection = new InfiniteScrollCollection<CarViewModel>(new PagingInfo { ItemsPerPage = 30 })
                .WithPageLoader(p => Task.FromResult(new PaginationSet<CarDto>(p, CreatePage(p, 65), 65, 65)))
                .WithMapping(MapToViewModel);

            // Assert: false until the first load completes, so the scroll behavior does not race InitializeAsync.
            collection.CanLoadMore.Should().BeFalse();
        }

        [Fact]
        public async Task LoadMoreAsync_EmptyPage_PublishesMetadata_ButRaisesNoCollectionChanged()
        {
            // Arrange: a zero-result page.
            var pagingInfo = new PagingInfo { ItemsPerPage = 30 };
            var collection = new InfiniteScrollCollection<CarViewModel>(pagingInfo)
                .WithPageLoader(p => Task.FromResult(new PaginationSet<CarDto>(p, Array.Empty<CarDto>(), 0, 0)))
                .WithMapping(MapToViewModel);

            var collectionChanged = new List<NotifyCollectionChangedEventArgs>();
            collection.CollectionChanged += (_, e) => collectionChanged.Add(e);

            // Act
            await collection.InitializeAsync();

            // Assert: no spurious Add event for the empty page, but the server totals are still published.
            collectionChanged.Should().BeEmpty();
            collection.Should().BeEmpty();
            collection.PaginationSet.Should().NotBeNull();
            collection.PaginationSet!.TotalCount.Should().Be(0);
            collection.CanLoadMore.Should().BeFalse();
        }

        [Fact]
        public async Task InitializeAsync_Throws_WhenProjectingLoaderNotFinalizedWithMapping()
        {
            // Arrange: WithPageLoader<TSource> was begun but WithMapping was never chained, so nothing is configured.
            var collection = new InfiniteScrollCollection<CarViewModel>(new PagingInfo());
            collection.WithPageLoader(p => Task.FromResult(new PaginationSet<CarDto>())); // returned source discarded

            // Act
            Func<Task> act = () => collection.InitializeAsync();

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task WithPageLoader_ExplicitTypeArgument_SameType_BindsDirectlyWithoutMapping()
        {
            // Arrange: an explicit type argument forces the generic overload even though TSource == TTarget.
            // No mapping is chained; the loaded type already is the bound type, so it must bind directly.
            const int totalCount = 30;
            var pagingInfo = new PagingInfo { ItemsPerPage = 30 };
            var collection = new InfiniteScrollCollection<CarDto>(pagingInfo);
            collection.WithPageLoader<CarDto>(p => Task.FromResult(new PaginationSet<CarDto>(p, CreatePage(p, totalCount), totalCount, totalCount)));

            // Act
            await collection.InitializeAsync();

            // Assert: loaded directly, no throw, no mapping required
            collection.Should().HaveCount(30);
            collection.First().Id.Should().Be(0);
        }

        [Fact]
        public async Task InitializeAsync_DoesNothing_OnSeededCollectionWithoutLoader()
        {
            // Arrange: a seeded collection has no page loader and no pending projection.
            var collection = new InfiniteScrollCollection<CarViewModel>(Cars.CreateCarViewModels(3));

            // Act + Assert: loading is a no-op, not an error.
            await collection.InitializeAsync();
            collection.Should().HaveCount(3);
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

        private static Task<IReadOnlyList<CarViewModel>> MapDtosToViewModels(IReadOnlyList<CarDto> dtos)
        {
            return Task.FromResult<IReadOnlyList<CarViewModel>>(dtos.Select(MapToViewModel).ToArray());
        }
    }
}
