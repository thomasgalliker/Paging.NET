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

`PagingInfo.DefaultFirstPageIndex` is a library constant. It is not configurable at runtime.
If you want zero-based paging, set `FirstPageIndex = 0` on the request itself. If you want one-based paging, use the
default `FirstPageIndex = 1`.

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
to create a `PaginationSet<T>` through `CreatePaginationSet<TEntity>(..)` extension method.

### How to Use Paging.Queryable.NET

`Paging.Queryable.NET` provides extensions for applying paging directly to an `IQueryable<T>`.
This is useful for backend code working with Entity Framework or any other LINQ provider.

The main entry point is `CreatePaginationSet<TEntity>(...)`. It applies the `PagingInfo` request to an 
`IQueryable<TEntity>` and returns a `PaginationSet<TEntity>`.

The following example shows how a `PaginationSet<Car>` can be created directly from an `IQueryable<Car>`:

```csharp
IQueryable<Car> queryable = dbContext.Cars;

var pagingInfo = new PagingInfo
{
    CurrentPage = 1,
    ItemsPerPage = 10,
    SortBy = "Year Desc, Name Asc",
    Search = "Model Desc",
    Filter =
    {
        { "Year", 2024 }
    }
};

var paginationSet = pagingInfo.CreatePaginationSet<Car>(queryable);
```

In this example:

- `Search` is applied using the provided search predicate.
- `Filter` applies additional property-based constraints.
- `SortBy` defines the ordering before paging is applied.
- The result is returned as a `PaginationSet<Car>`.
- `ItemsPerPage = 0` returns counts without materializing page items.
- `ItemsPerPage = null` skips `Skip(...).Take(...)` and returns all matching items.

#### Mapping Entities to DTOs

If entities should be mapped to DTOs, `CreatePaginationSet<TEntity, TDto>(...)` can be used to apply paging and map the
resulting page in one step:

```csharp
IQueryable<Car> queryable = dbContext.Cars;

var pagingInfo = new PagingInfo
{
    CurrentPage = 1,
    ItemsPerPage = 10,
    SortBy = "Year Desc, Name Asc",
    Search = "Model Desc",
    Filter =
    {
        { "Year", 2024 }
    }
};

var paginationSet = pagingInfo.CreatePaginationSet<Car, CarDto>(
    queryable,
    cars => cars.Select(car => new CarDto
    {
        Id = car.Id,
        Name = car.Name,
        Model = car.Model,
        Price = car.Price,
        Year = car.Year
    }));
```

This overload returns a `PaginationSet<CarDto>` instead of `PaginationSet<Car>`, while preserving the paging metadata.
If preferred, sorting can also be defined with the `Sorting` property instead of `SortBy`:

```csharp
var pagingInfo = new PagingInfo
{
    CurrentPage = 1,
    ItemsPerPage = 10,
    Sorting = new Dictionary<string, SortOrder>
    {
        { "Name", SortOrder.Asc },
        { "Year", SortOrder.Desc }
    }
};
```

The `Filter` dictionary supports simple property-based filtering. For example:

```csharp
var pagingInfo = new PagingInfo
{
    Filter = new Dictionary<string, object?>
    {
        { "Name", "Tesla" },
        { "Year", 2024 }
    }
};
```

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
