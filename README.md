# Paging.NET
[![Version](https://img.shields.io/nuget/v/Paging.NET.svg)](https://www.nuget.org/packages/Paging.NET)  [![Downloads](https://img.shields.io/nuget/dt/Paging.NET.svg)](https://www.nuget.org/packages/Paging.NET) [![Buy Me a Coffee](https://img.shields.io/badge/support-buy%20me%20a%20coffee-FFDD00)](https://buymeacoffee.com/thomasgalliker)

Paging.NET is a lightweight and flexible library for server-side paging and incremental data loading. Large datasets can be handled more efficiently by retrieving items in smaller, predictable chunks, making data access easier to manage. The library set consists of the following NuGet packages:

- **`Paging.NET`**: Core library containing the main paging models such as `PagingInfo` and `PaginationSet`.
- **`Paging.Queryable.NET`**: Extension library providing `IQueryable` support for paging, sorting, and filtering.
- **`Paging.MAUI`**: Add-on for .NET MAUI moble apps for implementing incremental loading and infinite scrolling scenarios.

### Download and Install Paging.NET
This library is available on nuget.org:

| Package                                                                     | Version                                                                                                                    | Downlods                                                                                                                      |
|-----------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------|
| [Paging.NET](https://www.nuget.org/packages/Paging.NET)                     | [![Version](https://img.shields.io/nuget/v/Paging.NET.svg)](https://www.nuget.org/packages/Paging.NET)                     | [![Downloads](https://img.shields.io/nuget/dt/Paging.NET.svg)](https://www.nuget.org/packages/Paging.NET)                     |
| [Paging.Queryable.NET](https://www.nuget.org/packages/Paging.Queryable.NET) | [![Version](https://img.shields.io/nuget/v/Paging.Queryable.NET.svg)](https://www.nuget.org/packages/Paging.Queryable.NET) | [![Downloads](https://img.shields.io/nuget/dt/Paging.Queryable.NET.svg)](https://www.nuget.org/packages/Paging.Queryable.NET) |
| [Paging.MAUI](https://www.nuget.org/packages/Paging.MAUI)                   | [![Version](https://img.shields.io/nuget/v/Paging.MAUI.svg)](https://www.nuget.org/packages/Paging.MAUI)                   | [![Downloads](https://img.shields.io/nuget/dt/Paging.MAUI.svg)](https://www.nuget.org/packages/Paging.MAUI)                   |


### API Usage
Paging or pagination is a process of slicing a certain (usually big and costly) collection into subsets of items in order to improve query performance. Paging is not only a matter of splitting collections into chunks, it also has to consider sorting and filtering. Paging involves the requesting client in specifying a paging request and the responding service to respond with a result set.

In Paging.NET, each paging request is specified in a `PagingInfo`. The resulting page is returned in a `PaginationSet`.

- **`PagingInfo`** allows to define which page index we want to retrieve (`CurrentPage`), how many items each page shall contain (`ItemsPerPage`), how the collection is sorted before it is paged (`SortBy` resp. `Sorting`) and if we like to apply a filter (`Search` resp. `Filter`) on the target collection.
- **`PaginationSet`** sends the subset of `Items` along with some meta information, like the current page's zero-based index `CurrentPage`, the total number of pages `TotalPages`, the total number of items `TotalCount` (unfiltered: `TotalCountUnfiltered`).


```
TODO: Document the usage of PagingInfo and PaginationSet in some concrete examples
```

### Contribution
Contributors welcome! If you find a bug or you want to propose a new feature, feel free to do so by opening a new issue on github.com.
