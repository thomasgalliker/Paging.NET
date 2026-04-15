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

The core library defines the two main types:

- **`PagingInfo`** is the paging request model. It specifies which page should be loaded, how many items should be
  returned,
  and which sorting, search, or filtering options should be applied.

| Property       | Description                                                                                                                     |
|----------------|---------------------------------------------------------------------------------------------------------------------------------|
| `CurrentPage`  | The currently selected page.<br/> The default value is `1`.                                                                     |
| `ItemsPerPage` | Number of items returned per page. <br/>The default value is `0`, which means all matching items are returned in a single page. |
| `SortBy`       | Comma-separated sort expression such as `"Name Asc"` or `"Year Desc, Name Asc"`.                                                |
| `Sorting`      | Dictionary-based sort definition as an alternative to `SortBy` string.                                                          |
| `Reverse`      | Reverses the final sort order.                                                                                                  |
| `Search`       | Free-text search value that can be applied by the target data source.                                                           |
| `Filter`       | Property-based filter values that can be applied by the target data source.                                                     |

- **`PaginationSet<T>`** is the paged response model. It contains the items of the current page together with metadata
  describing the complete result set.

| Property               | Description                                                                   |
|------------------------|-------------------------------------------------------------------------------|
| `CurrentPage`          | The current page number of the returned result. This value is also `1`-based. |
| `TotalPages`           | Total number of pages available for the current filter and search criteria.   |
| `TotalCount`           | Total number of items matching the current filter and search criteria.        |
| `TotalCountUnfiltered` | Total number of items before filter or search is applied.                     |
| `Items`                | The items contained in the current page.                                      |

The following example shows a simple request using the core models:

```csharp
var pagingInfo = new PagingInfo
{
    CurrentPage = 1,
    ItemsPerPage = 10,
    SortBy = "Name Asc",
    Search = "Model Desc"
};
```

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
