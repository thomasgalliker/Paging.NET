# Paging.NET

[![Version](https://img.shields.io/nuget/v/Paging.NET.svg)](https://www.nuget.org/packages/Paging.NET)  [![Downloads](https://img.shields.io/nuget/dt/Paging.NET.svg)](https://www.nuget.org/packages/Paging.NET) [![Buy Me a Coffee](https://img.shields.io/badge/support-buy%20me%20a%20coffee-FFDD00)](https://buymeacoffee.com/thomasgalliker)

Paging.NET is a lightweight and flexible library for server-side paging and incremental data loading. Large datasets can
be handled more efficiently by retrieving items in smaller, predictable chunks, making data access easier to manage. The
library set consists of the following NuGet packages:

- **`Paging.NET`**: Core library containing the main paging models such as `PagingInfo` and `PaginationSet`.
- **`Paging.Queryable.NET`**: Extension library providing `IQueryable` support for paging, sorting, and filtering.
- **`Paging.MAUI`**: Add-on for .NET MAUI moble apps for implementing incremental loading and infinite scrolling
  scenarios.

## Download and Install Paging.NET

This library is available on nuget.org:

| Package                                                                     | Version                                                                                                                    | Downlods                                                                                                                      |
|-----------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------|
| [Paging.NET](https://www.nuget.org/packages/Paging.NET)                     | [![Version](https://img.shields.io/nuget/v/Paging.NET.svg)](https://www.nuget.org/packages/Paging.NET)                     | [![Downloads](https://img.shields.io/nuget/dt/Paging.NET.svg)](https://www.nuget.org/packages/Paging.NET)                     |
| [Paging.Queryable.NET](https://www.nuget.org/packages/Paging.Queryable.NET) | [![Version](https://img.shields.io/nuget/v/Paging.Queryable.NET.svg)](https://www.nuget.org/packages/Paging.Queryable.NET) | [![Downloads](https://img.shields.io/nuget/dt/Paging.Queryable.NET.svg)](https://www.nuget.org/packages/Paging.Queryable.NET) |
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

| Property       | Description                                                                                                                     |
|----------------|---------------------------------------------------------------------------------------------------------------------------------|
| `FirstPageIndex` | The first valid page index for the request. Allowed values are `0` and `1`. |
| `CurrentPage`  | The currently selected page.<br/> The default value is `PagingInfo.DefaultFirstPageIndex`, which means it initially matches `FirstPageIndex`. |
| `ItemsPerPage` | Number of items returned per page. <br/>`null` disables paging and returns all items, `0` returns totals only, and positive values enable normal paging. The static `PagingInfo.DefaultItemsPerPage` defaults to `null`. |
| `SortBy`       | Comma-separated sort expression such as `"Name Asc"` or `"Year Desc, Name Asc"`.                                                |
| `Sorting`      | Dictionary-based sort definition as an alternative to `SortBy` string.                                                          |
| `Reverse`      | Reverses the final sort order.                                                                                                  |
| `Search`       | Free-text search value that can be applied by the target data source.                                                           |
| `Filter`       | Property-based filter values that can be applied by the target data source.                                                     |

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
request and response contract and is serialized over JSON and query strings when it differs from `PagingInfo.DefaultFirstPageIndex`.

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

If you are working with an `IQueryable<T>`, the section below shows how `Paging.Queryable.NET` can perform all necessary steps
to create a `PaginationSet<T>` through the `ToPaginationSet(...)` extension method.

### How to Use Paging.Queryable.NET

`Paging.Queryable.NET` provides extensions for applying paging directly to an `IQueryable<T>`.
This is useful for backend code working with Entity Framework or any other LINQ provider.

The main entry point is `ToPaginationSet(...)`. It applies the `PagingInfo` request to an
`IQueryable<TEntity>` and returns a `PaginationSet<TEntity>`:

```csharp
IQueryable<Car> queryable = dbContext.Cars;

var pagingInfo = new PagingInfo
{
    CurrentPage = 1,
    ItemsPerPage = 10,
    SortBy = "Year Desc, Name Asc",
    Filter =
    {
        { "Year", 2024 }
    }
};

var paginationSet = queryable.ToPaginationSet(pagingInfo);
```

In this example:

- `Filter` applies property-based constraints before counting and paging.
- `SortBy` defines the ordering before paging is applied.
- The result is returned as a `PaginationSet<Car>`.
- `ItemsPerPage = 0` returns counts without materializing page items.
- `ItemsPerPage = null` skips `Skip(...).Take(...)` and returns all matching items.

Without further configuration, `ToPaginationSet(pagingInfo)` behaves permissively:

- Every entity property is sortable and filterable by its (dotted) property path, e.g. `"Name"` or `"Owner.Name"`.
  Property names are matched case-insensitively.
- Sort or filter names which cannot be resolved are silently skipped.
- `PagingInfo.Search` is ignored (a search predicate must be configured via `PagingOptions`).
- No default sort order is applied. **Configure `PagingOptions.DefaultSort` for deterministic paging** —
  without a stable sort order, the database may return rows in arbitrary order between page requests.

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

    // Map entities to DTOs (mandatory on PagingOptions<TEntity, TDto>)
    o.Map(cars => cars.Select(car => new CarDto
    {
        Id = car.Id,
        Name = car.Name,
        Model = car.Model,
        Price = car.Price,
        Year = car.Year
    }));
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
        this.Map(mapper.MapCarsToCarDtos);
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

When `PagingOptions` are used, unknown sort/filter property names **throw a `PagingException`** by default.
Web APIs typically translate this exception into an HTTP 400 (Bad Request) response.
The behavior is configurable, globally or separately for sorting and filtering:

```csharp
o.UnknownProperties(UnknownPropertyHandling.Ignore);      // sets both
o.UnknownSortProperties(UnknownPropertyHandling.Throw);
o.UnknownFilterProperties(UnknownPropertyHandling.Allow);
```

| Handling | Behavior                                                                                       |
|----------|------------------------------------------------------------------------------------------------|
| `Throw`  | Throws `PagingException` (with `PropertyName`) for unknown property names. **Default.**         |
| `Ignore` | Silently skips unknown property names.                                                          |
| `Allow`  | Resolves unknown names as (dotted) entity property paths; unresolvable names are skipped.       |

#### Filter Value Semantics

The `Filter` dictionary of `PagingInfo` supports the following value types:

| Filter value                                | Behavior                                                              |
|---------------------------------------------|------------------------------------------------------------------------|
| `{ "Name", "Tesla" }`                       | Case-insensitive contains search.                                      |
| `{ "Price", ">=5000" }`                     | Comparison using a leading operator (`>`, `>=`, `<`, `<=`, `=`, `==`). |
| `{ "Year", 2024 }`                          | Equality for numeric, `bool` and `DateTime` values.                    |
| `{ "Date", new Dictionary<string, object> { { ">", "2024-01-01" } } }` | Range comparisons with operator/value pairs.    |
| `{ "Id", new[] { 1, 2, 3 } }`               | IN-filter; matches any of the provided values.                         |

Invalid filter values (e.g. a non-numeric string compared to a numeric property) are leniently skipped.

#### Migration from 4.x to 5.0

| 4.x                                                         | 5.0                                                                            |
|-------------------------------------------------------------|----------------------------------------------------------------------------------|
| `pagingInfo.CreatePaginationSet(queryable)`                 | `queryable.ToPaginationSet(pagingInfo)`                                           |
| `pagingInfo.CreatePaginationSet(queryable, map, predicate)` | `queryable.ToPaginationSet(pagingInfo, pagingOptions)` with `Map(...)`/`Search(...)` configured in `PagingOptions<TEntity, TDto>` |
| `queryable.ApplyFilter(filter)` / `queryable.OrderBy(sorting, reverse)` | Removed; handled internally by `ToPaginationSet`.                     |
| `queryable.OrderByDefault()` (implicit `OrderBy("0")`)      | Removed; configure `PagingOptions.DefaultSort` for deterministic paging.          |
| Dependency on `System.Linq.Dynamic.Core`                    | Removed; `Paging.Queryable.NET` is dependency-free.                               |

### How to Use Paging.MAUI

`Paging.MAUI` provides helpers for incremental loading and infinite scrolling in .NET MAUI apps.
The central type is `InfiniteScrollCollection<T>`. It is typically used together with a `PagingInfo` instance that keeps
track of the next page to load.

The following example is based on the MAUI sample app:

```csharp
private readonly PagingInfo pagingInfo = new PagingInfo
{
    CurrentPage = 1,
    ItemsPerPage = 30,
};

private PaginationSet<Car>? lastPaginationSet;

public InfiniteScrollCollection<CarDto> Cars { get; } = new InfiniteScrollCollection<CarDto>();

public async Task InitializeAsync(ICarService carService)
{
    this.Cars.OnCanLoadMore = () => !this.lastPaginationSet.StopScroll(this.pagingInfo);
    this.Cars.OnLoadMore = async () =>
    {
        var paginationSet = await carService.GetCarsAsync(this.pagingInfo);
        this.lastPaginationSet = paginationSet;
        this.pagingInfo.CurrentPage++;

        return paginationSet.Items.Select(car => new CarDto
        {
            Id = car.Id,
            Name = car.Name,
            Model = car.Model,
            Price = car.Price,
            Year = car.Year
        });
    };

    await this.Cars.LoadMoreAsync();
}
```

This pattern assumes normal paging with `ItemsPerPage > 0`. For totals-only or unpaged requests, `StopScroll(...)`
returns `true` immediately.

In XAML, `InfiniteScrollBehavior` can be attached to a `ListView`:

```xml

<ListView ItemsSource="{Binding Cars}">
    <ListView.Behaviors>
        <paging:InfiniteScrollBehavior
                ItemsSource="{Binding Cars}"
                IsLoadingMore="{Binding IsLoadingMore}"/>
    </ListView.Behaviors>
</ListView>
```

When the user scrolls to the last item, the next page is loaded automatically as long as `OnCanLoadMore` returns `true`.

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
