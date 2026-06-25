# Paging.NET

[![Version](https://img.shields.io/nuget/v/Paging.NET.svg)](https://www.nuget.org/packages/Paging.NET)  [![Downloads](https://img.shields.io/nuget/dt/Paging.NET.svg)](https://www.nuget.org/packages/Paging.NET) [![Buy Me a Coffee](https://img.shields.io/badge/support-buy%20me%20a%20coffee-FFDD00)](https://buymeacoffee.com/thomasgalliker)

Paging.NET is a lightweight and flexible library for server-side paging and incremental data loading. Large datasets can
be handled more efficiently by retrieving items in smaller, predictable chunks, making data access easier to manage. The
library set consists of the following NuGet packages:

- **`Paging.NET`**: Core library containing the main paging models such as `PagingInfo` and `PaginationSet`.
- **`Paging.Queryable.NET`**: Extension library providing `IQueryable` support for paging, sorting, and filtering.
- **`Paging.EF`**: Add-on providing asynchronous Entity Framework Core terminals (`ToPaginationSetAsync`).
- **`Paging.MAUI`**: Add-on for .NET MAUI moble apps for implementing incremental loading and infinite scrolling
  scenarios.

## Download and Install Paging.NET

This library is available on nuget.org:

| Package                                                                     | Version                                                                                                                    | Downlods                                                                                                                      |
|-----------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------|
| [Paging.NET](https://www.nuget.org/packages/Paging.NET)                     | [![Version](https://img.shields.io/nuget/v/Paging.NET.svg)](https://www.nuget.org/packages/Paging.NET)                     | [![Downloads](https://img.shields.io/nuget/dt/Paging.NET.svg)](https://www.nuget.org/packages/Paging.NET)                     |
| [Paging.Queryable.NET](https://www.nuget.org/packages/Paging.Queryable.NET) | [![Version](https://img.shields.io/nuget/v/Paging.Queryable.NET.svg)](https://www.nuget.org/packages/Paging.Queryable.NET) | [![Downloads](https://img.shields.io/nuget/dt/Paging.Queryable.NET.svg)](https://www.nuget.org/packages/Paging.Queryable.NET) |
| [Paging.EF](https://www.nuget.org/packages/Paging.EF)                       | [![Version](https://img.shields.io/nuget/v/Paging.EF.svg)](https://www.nuget.org/packages/Paging.EF)                       | [![Downloads](https://img.shields.io/nuget/dt/Paging.EF.svg)](https://www.nuget.org/packages/Paging.EF)                       |
| [Paging.MAUI](https://www.nuget.org/packages/Paging.MAUI)                   | [![Version](https://img.shields.io/nuget/v/Paging.MAUI.svg)](https://www.nuget.org/packages/Paging.MAUI)                   | [![Downloads](https://img.shields.io/nuget/dt/Paging.MAUI.svg)](https://www.nuget.org/packages/Paging.MAUI)                   |

## Getting Started

Paging or pagination is the process of splitting a collection into smaller subsets of items in order to improve
performance and reduce the amount of data transferred at once. In practice, paging is usually combined with sorting,
searching, and filtering.

In Paging.NET, the client sends a paging request as a `PagingInfo`, and the service responds with a `PaginationSet<T>`.

### How to Use Paging.NET

#### Core Types

`Paging.NET` is built around two core models:

- **`PagingInfo`** is the paging request model. It specifies which page should be loaded, how many items should be
  returned, and which sorting, search, or filtering options should be applied.

| Property         | Description                                                                                                                                                                                                              |
|------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `FirstPageIndex` | The first valid page index for the request. Allowed values are `0` and `1`.                                                                                                                                              |
| `CurrentPage`    | The currently selected page.<br/> The default value is `PagingInfo.DefaultFirstPageIndex`, which means it initially matches `FirstPageIndex`.                                                                            |
| `ItemsPerPage`   | Number of items returned per page. <br/>`null` disables paging and returns all items, `0` returns totals only, and positive values enable normal paging. The static `PagingInfo.DefaultItemsPerPage` defaults to `null`. |
| `SortBy`         | Comma-separated sort expression such as `"Name Asc"` or `"Year Desc, Name Asc"`.                                                                                                                                         |
| `Sorting`        | Dictionary-based sort definition as an alternative to `SortBy` string.                                                                                                                                                   |
| `Reverse`        | Reverses the final sort order.                                                                                                                                                                                           |
| `Search`         | Free-text search value that can be applied by the target data source.                                                                                                                                                    |
| `Filter`         | A `FilterNode` (single `FilterCondition` or nested `FilterGroup`) that serializes as a compact filter expression string, e.g. `Year >= 2020 && Name contains "bmw"`. `null` means no filtering.                          |

- **`PaginationSet<T>`** is the paged response model. It contains the items of the current page together with metadata
  describing the complete result set.

| Property               | Description                                                                                 |
|------------------------|---------------------------------------------------------------------------------------------|
| `FirstPageIndex`       | The first page index used for the request and response.                                     |
| `CurrentPage`          | The current page number of the returned result. This value is relative to `FirstPageIndex`. |
| `TotalPages`           | Total number of pages available for the current filter and search criteria.                 |
| `TotalCount`           | Total number of items matching the current filter and search criteria.                      |
| `TotalCountUnfiltered` | Total number of items before filter or search is applied.                                   |
| `Items`                | The items contained in the current page.                                                    |

#### Basic Example

The following example shows a simple request using the core models:

```csharp
var pagingInfo = new PagingInfo
{
    FirstPageIndex = PagingInfo.DefaultFirstPageIndex,
    CurrentPage = 1,
    ItemsPerPage = 10,
    SortBy = "Name Asc",
    Search = "Model Desc"
};
```

#### Paging Defaults and Modes

You can configure the global default page size once for your process:

##### Disable Paging By Default

```csharp
PagingInfo.DefaultItemsPerPage = null; // null means return all items - no paging is used
```

##### Choose the First Page Index (0- or 1-based)

`PagingInfo.DefaultFirstPageIndex` is a global library default. Allowed values are `0` and `1`.
If you want zero-based paging by default, set `PagingInfo.DefaultFirstPageIndex = 0`. You can still override
`FirstPageIndex` per request.

```csharp
PagingInfo.DefaultFirstPageIndex = 0;
```

Or set it directly on a request:

```csharp
var pagingInfo = new PagingInfo
{
    FirstPageIndex = 0,
    CurrentPage = 0,
    ItemsPerPage = 10
};
```

`FirstPageIndex` is part of the `PagingInfo` contract and is serialized automatically when it differs from
`PagingInfo.DefaultFirstPageIndex`.

`ItemsPerPage` has three explicit modes:

- `null`: disable paging and return all matching items
- `0`: return totals only and zero items
- `> 0`: return the requested page with the requested number of items

`CurrentPage` is interpreted relative to `FirstPageIndex`, which may be `0` or `1`. `FirstPageIndex` is part of the
request and response contract and is serialized over JSON and query strings when it differs from
`PagingInfo.DefaultFirstPageIndex`.

#### Service Example

A service can then use this request to return a page of `Car` items. The following example is intentionally kept as
pseudo code to illustrate the general flow:

```csharp
public PaginationSet<Car> GetCars(PagingInfo pagingInfo)
{
    var query = LoadCars();

    // Apply search, filtering, sorting, grouping of data
    // Apply Skip(...).Take(...) for the requested page

    return new PaginationSet<Car>(pagingInfo, pageItems, totalCount, totalCount);
}
```

If you are working with an `IQueryable<T>`, the section below shows how `Paging.Queryable.NET` can perform all necessary
steps to create a `PaginationSet<T>` through the `ToPaginationSet(...)` extension method.

### How to Use Paging.Queryable.NET

`Paging.Queryable.NET` provides extensions for applying paging directly to an `IQueryable<T>`.
This is useful for backend code working with Entity Framework or any other LINQ provider.

The main entry point is `ToPaginationSet(...)`. It applies the `PagingInfo` request to an
`IQueryable<TEntity>` and returns a `PaginationSet<TEntity>`.

```csharp
IQueryable<Car> queryable = dbContext.Cars;

var pagingInfo = new PagingInfo
{
    CurrentPage = 1,
    ItemsPerPage = 10,
    SortBy = "Year Desc, Name Asc",
    Filter = FilterNode.Parse("Year == 2024")
};

var paginationSet = queryable.ToPaginationSet(pagingInfo);
```

In this example:

- `Filter` applies property-based constraints before counting and paging.
- `SortBy` defines the ordering before paging is applied.
- The result is returned as a `PaginationSet<Car>`.
- `ItemsPerPage = 0` returns counts without materializing page items.
- `ItemsPerPage = null` skips `Skip(...).Take(...)` and returns all matching items.

Sorting and filtering are **allow-list only**: a property can only be sorted or filtered if it is declared in
`PagingOptions` (see below). This keeps the backend in full control — the request carries only property names,
operators and values, never an expression tree. Unknown names throw a `PagingException` by default (configurable
to `Ignore`). **Configure `PagingOptions.DefaultSort` for deterministic paging** — without a stable sort order,
the database may return rows in arbitrary order between page requests.

The no-options `queryable.ToPaginationSet(pagingInfo)` overload performs **paging only** (count + `Skip`/`Take`) on
an already-shaped query; it does not filter or sort. Use it together with the `ApplyPaging(...)` composition seam
(see [Composing and projecting](#composing-projecting-and-async)).

#### Configuring Sorting, Filtering and Mapping with PagingOptions

`PagingOptions<TEntity>` controls which properties are exposed for sorting and filtering, how external
(client-facing) names map to entity properties, computed sort expressions, custom filter predicates,
free-text search and the default sort order. `PagingOptions<TEntity, TDto>` additionally maps the queried
entities to DTOs.

```csharp
var pagingOptions = new PagingOptions<Car, CarDto>(o =>
{
    // Each property declares its capabilities explicitly;
    // the external name defaults to the property path
    o.Property(c => c.Name).Sortable().Filterable();

    // Map an external name to an entity property (nested paths supported)
    o.Property(c => c.Model).HasName("Brand").Sortable().Filterable();
    o.Property(c => c.Owner.Name).HasName("Owner").Sortable();   // sort-only

    // Computed sort key: the client sorts by "Age", which has no database column
    o.Property("Age").Sortable(c => DateTime.UtcNow.Year - c.Year);

    // Custom filter predicate for arbitrary filter values
    o.Property("Electric").Filterable(value => value is bool isElectric
        ? (Expression<Func<Car, bool>>)(c => c.IsElectric == isElectric)
        : null);

    // Stable default sort: primary order when no sorting is requested
    // AND tie-breaker appended after any requested sort order
    o.DefaultSort(c => c.Id, SortOrder.Desc);

    // Free-text search predicate; receives PagingInfo.Search
    o.Search(s => c => c.Name.ToLower().Contains(s.ToLower()));

    // Project entities to DTOs (mandatory on PagingOptions<TEntity, TDto>).
    // The expression is applied via Select(...) and translated to SQL,
    // so only the projected columns are fetched.
    o.Map(car => new CarDto
    {
        Id = car.Id,
        Name = car.Name,
        Model = car.Model,
        Price = car.Price,
        Year = car.Year
    });
});

var paginationSet = queryable.ToPaginationSet(pagingInfo, pagingOptions);
```

`PagingOptions` is frozen on first use and can safely be cached and reused across queries and threads.

#### Class-Based PagingOptions with Dependency Injection

Instead of configuring inline, `PagingOptions` can be subclassed and registered in dependency injection.
Constructor injection is the recommended way to feed runtime values (such as the current date/time)
into computed sort expressions. Use the factory overload of `Sortable` to evaluate the expression
freshly on every query:

```csharp
public class CarPagingOptions : PagingOptions<Car, CarDto>
{
    public CarPagingOptions(IDateTime dateTime, ICarMapper mapper)
    {
        this.Property(c => c.Name).Sortable().Filterable();
        this.Property(c => c.Model).HasName("Brand").Sortable().Filterable();

        this.Property("Age").Sortable(() =>
        {
            var now = dateTime.UtcNow;
            return (Expression<Func<Car, int>>)(c => now.Year - c.Year);
        });

        this.DefaultSort(c => c.Id, SortOrder.Desc);
        this.Map(car => mapper.MapCarToCarDto(car));
    }
}

// Startup:
services.AddScoped<CarPagingOptions>();

// Controller:
var paginationSet = dbContext.Cars.ToPaginationSet(pagingInfo, this.carPagingOptions);
```

All sort and filter expressions remain LINQ expression trees, so Entity Framework translates them
to SQL — including computed sort keys (e.g. `CASE` expressions) and captured runtime values
(which become SQL parameters).

#### Handling of Unknown Property Names

Unknown sort/filter property names (names not declared in `PagingOptions`) **throw a `PagingException`** by default.
Web APIs typically translate this exception into an HTTP 400 (Bad Request) response.
The behavior is configurable, globally or separately for sorting and filtering:

```csharp
o.UnknownProperties(UnknownPropertyHandling.Ignore);      // sets both
o.UnknownSortProperties(UnknownPropertyHandling.Throw);
o.UnknownFilterProperties(UnknownPropertyHandling.Ignore);
```

| Handling | Behavior                                                                                |
|----------|-----------------------------------------------------------------------------------------|
| `Throw`  | Throws `PagingException` (with `PropertyName`) for unknown property names. **Default.** |
| `Ignore` | Silently skips unknown property names.                                                  |

There is intentionally no mode that resolves arbitrary client-supplied property paths against the entity. The backend
always declares — via `PagingOptions` — exactly which properties are sortable/filterable and what each maps to.

#### Filter Expressions

`PagingInfo.Filter` is a `FilterNode` that **serializes as a compact filter expression string** —
URL- and JSON-friendly, so an Angular or MAUI client can send it as a query parameter or in a request body.
The string supports comparisons, string operators, `in`, and `&&` / `||` with parentheses
(`&&` binds tighter than `||`):

`Brand contains "bmw" && Year >= 2020 || IsElectric == true`

You build it in three interchangeable ways — the wire form is always the string:

```csharp
// 1. Assign a string directly (implicitly parsed), or call FilterNode.Parse(...) explicitly
pagingInfo.Filter = "Brand contains \"bmw\" && Year >= 2020";
pagingInfo.Filter = FilterNode.Parse("Brand contains \"bmw\" && Year >= 2020");

// 2. Build the typed tree (type-safe in .NET / MAUI); it serializes to the same string
pagingInfo.Filter = FilterGroup.And(
    new FilterCondition("Brand", FilterOperator.Contains, "bmw"),
    new FilterCondition("Year", FilterOperator.GreaterThanOrEqual, 2020));

// 3. Send it as JSON — the value is the expression string
//    { "filter": "Brand contains \"bmw\" && Year >= 2020" }
```

Operators and their string tokens:

| Operator             | Token        | Behavior                                                                 |
|----------------------|--------------|--------------------------------------------------------------------------|
| `Equal`              | `==`         | Property equals the value.                                               |
| `NotEqual`           | `!=`         | Property does not equal the value.                                       |
| `GreaterThan`        | `>`          | Property is greater than the value.                                      |
| `GreaterThanOrEqual` | `>=`         | Property is greater than or equal to the value.                          |
| `LessThan`           | `<`          | Property is less than the value.                                         |
| `LessThanOrEqual`    | `<=`         | Property is less than or equal to the value.                             |
| `Contains`           | `contains`   | Case-insensitive substring match (non-string properties use `ToString`). |
| `StartsWith`         | `startswith` | Case-insensitive prefix match.                                           |
| `EndsWith`           | `endswith`   | Case-insensitive suffix match.                                           |
| `In`                 | `in`         | Property matches any value of a `[…]` list, e.g. `Id in [1, 2, 3]`.      |

Value literals: quoted strings (`"text"`, `\"` escapes a quote), numbers (`42`, `4.5`), `true`, `false`,
`null`, and bracketed lists for `in`. Dates are passed as quoted ISO 8601 strings.

Each property name must be declared `Filterable()` in `PagingOptions` (or have a custom `Filterable(...)`
predicate) — the parser only produces names; the backend decides what each maps to. Syntactically invalid
expressions throw a `FormatException` (surfaced as an HTTP 400 during model binding); values that cannot be
converted to the target property type are leniently skipped.

#### Composing, Projecting and Async

`ApplyPaging(...)` is the composition seam: it applies search, filter and sort and returns the shaped
`IQueryable<TEntity>` **without** `Skip`/`Take`. Compose further — for example project to a DTO so the
projection is translated to SQL — and then call the terminal `ToPaginationSet(...)` (or the async
`ToPaginationSetAsync(...)` from `Paging.EF`) to count and page:

```csharp
using Paging.EF; // ToPaginationSetAsync

var paginationSet = await dbContext.Cars
    .ApplyPaging(pagingInfo, pagingOptions)   // filter + sort, in SQL
    .Select(c => new CarDto { Id = c.Id, Name = c.Name })  // projection pushed to SQL
    .ToPaginationSetAsync(pagingInfo, cancellationToken);
```

The convenience overloads do the same in one call:

```csharp
// Synchronous
var set = dbContext.Cars.ToPaginationSet(pagingInfo, pagingOptions);

// Asynchronous (Paging.EF) — uses EF Core CountAsync/ToListAsync
var setAsync = await dbContext.Cars.ToPaginationSetAsync(pagingInfo, pagingOptions, cancellationToken);
```

By default `PaginationSet.TotalCountUnfiltered` equals `TotalCount` (no extra count query is issued).
Call `o.IncludeUnfilteredCount()` in `PagingOptions` to compute the unfiltered total with a separate count.

#### Mapping the Result In-Memory

Both projection styles seen so far run **in the database**: the inline `.Select(...)` above, and the
configured `PagingOptions<TEntity, TDto>.Map(...)` (which the typed `ToPaginationSet(...)` overload applies
as that same `.Select(...)`). Either way the projection is translated to SQL, so it only supports what EF Core
can translate. When you instead need to map the page with a runtime mapper that cannot be translated — for
example NMapper, AutoMapper, TinyMapper, Mapperly, or a mapper that handles object graphs / recursion — page
the entities first and then map the materialized `PaginationSet<TSource>` to a `PaginationSet<TTarget>` with
`Map(...)`:

```csharp
var paginationSet = (await dbContext.Cars.ToPaginationSetAsync(pagingInfo, pagingOptions, cancellationToken))
    .Map(cars => this.mapper.Map<CarDto[]>(cars));
```

`Map` applies the mapping function to `Items` and carries all paging metadata
(`FirstPageIndex`, `CurrentPage`, `TotalPages`, `TotalCount`, `TotalCountUnfiltered`) over unchanged, so the
counts stay correct. Prefer the SQL projection when the mapping is translatable (it pages and projects in a
single query); reach for in-memory `Map` only when it is not.

### How to Use Paging.MAUI

`Paging.MAUI` provides helpers for incremental loading and infinite scrolling in .NET MAUI apps.
The central type is `InfiniteScrollCollection<T>`.

#### Self-contained collection (recommended)

`InfiniteScrollCollection<TSource, TItem>` owns the `PagingInfo`, advances the page after each load, maps every
loaded item and decides when to stop. The view model only declares how to load a page (`pageLoader`) and how to
project it (`itemSelector`):

```csharp
public InfiniteScrollCollection<Car, CarItemViewModel> Cars { get; }

public MainViewModel(ICarService carService, ILogger<MainViewModel> logger)
{
    this.Cars = new InfiniteScrollCollection<Car, CarItemViewModel>(
        pageLoader: pagingInfo => carService.GetCarsAsync(pagingInfo),
        itemSelector: car => new CarItemViewModel(car),
        pagingInfo: new PagingInfo { ItemsPerPage = 30 })
    {
        // Set OnError so failures in the fire-and-forget first load (and in scroll-driven
        // loads, which the behavior runs as async void) are observed instead of lost.
        OnError = ex => logger.LogError(ex, "Failed to load cars"),
    };

    _ = this.Cars.InitializeAsync(); // load the first page
}
```

Call `RefreshAsync()` after changing `Cars.PagingInfo.Search`, `Filter` or `SortBy` to clear the list and reload
from the first page. When you bind the loaded type directly (no projection), use the single-generic constructor
— same shape, minus the `itemSelector`:

```csharp
public InfiniteScrollCollection<Car> Cars { get; }

public MainViewModel(ICarService carService, ILogger<MainViewModel> logger)
{
    this.Cars = new InfiniteScrollCollection<Car>(
        pageLoader: pagingInfo => carService.GetCarsAsync(pagingInfo),
        pagingInfo: new PagingInfo { ItemsPerPage = 30 })
    {
        OnError = ex => logger.LogError(ex, "Failed to load cars"),
    };

    _ = this.Cars.InitializeAsync();
}
```

#### Manual wiring (escape hatch)

For full control, keep your own `PagingInfo` and set the `OnLoadMore`/`OnCanLoadMore` delegates yourself. Use the
`CanLoadMore(...)` extension to decide whether another page is available:

```csharp
private readonly PagingInfo pagingInfo = new PagingInfo { ItemsPerPage = 30 };
private PaginationSet<Car>? lastPaginationSet;

public InfiniteScrollCollection<CarItemViewModel> Cars { get; } = new InfiniteScrollCollection<CarItemViewModel>();

public async Task InitializeAsync(ICarService carService)
{
    this.Cars.OnCanLoadMore = () => this.lastPaginationSet.CanLoadMore(this.pagingInfo);
    this.Cars.OnLoadMore = async () =>
    {
        var paginationSet = await carService.GetCarsAsync(this.pagingInfo);
        this.lastPaginationSet = paginationSet;
        this.pagingInfo.CurrentPage++;

        return paginationSet.Items
            .Select(car => new CarItemViewModel(car))
            .ToArray();
    };

    await this.Cars.LoadMoreAsync();
}
```

This pattern assumes normal paging with `ItemsPerPage > 0`. For totals-only or unpaged requests, `CanLoadMore(...)`
returns `false` immediately.

In XAML, `InfiniteScrollBehavior` can be attached to a `CollectionView`
(xmlns `paging` referring to `clr-namespace:Paging.MAUI;assembly=Paging.MAUI`):

```xml

<CollectionView ItemsSource="{Binding Cars}">
    <CollectionView.Behaviors>
        <paging:InfiniteScrollBehavior
                ItemsSource="{Binding Cars}"
                IsLoadingMore="{Binding IsLoadingMore}"
                RemainingItemsThreshold="5"/>
    </CollectionView.Behaviors>
</CollectionView>
```

The behavior uses CollectionView's native `RemainingItemsThresholdReached` mechanism:
when the user scrolls close to the end of the list (`RemainingItemsThreshold` items remaining,
default 5), the next page is loaded automatically as long as `OnCanLoadMore` returns `true`.
The threshold set on the behavior overwrites any `RemainingItemsThreshold` set directly
on the CollectionView.

If you prefer commands over the behavior, CollectionView's built-in incremental loading
can be used directly with `InfiniteScrollCollection` — at the cost of guard logic in every
view model (`RemainingItemsThresholdReached` fires repeatedly while scrolling):

```csharp
public IAsyncRelayCommand LoadMoreCommand => this.loadMoreCommand ??= new AsyncRelayCommand(async () =>
{
    if (this.Cars.IsLoadingMore || !this.Cars.CanLoadMore)
    {
        return;
    }

    await this.Cars.LoadMoreAsync();
});
```

```xml

<CollectionView ItemsSource="{Binding Cars}"
                RemainingItemsThreshold="5"
                RemainingItemsThresholdReachedCommand="{Binding LoadMoreCommand}"/>
```

#### Legacy ListView support

The former ListView-based `InfiniteScrollBehavior` is still available in the namespace
`Paging.MAUI.Compat` (xmlns `clr-namespace:Paging.MAUI.Compat;assembly=Paging.MAUI`).
ListView is deprecated in .NET MAUI; this behavior will be removed in a future release.

## Contribution

If you find a bug or want to propose a new feature, feel free to create a new
issue [here](https://github.com/thomasgalliker/Paging.NET/issues/new/choose).
Please use the predefined issue templates when submitting a new issue.

## Thank You

Your contribution is valuable!
Open source software isn’t just something you can pick up for free — it represents the hard work and dedication of many
people who often not even know each other.
We sincerely appreciate the time, effort, and dedication shown by everyone who helps keep this plugin going forward.

## Links
