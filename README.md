# Paging.NET
[![Version](https://img.shields.io/nuget/v/Paging.NET.svg)](https://www.nuget.org/packages/Paging.NET)  [![Downloads](https://img.shields.io/nuget/dt/Paging.NET.svg)](https://www.nuget.org/packages/Paging.NET) [![Buy Me a Coffee](https://img.shields.io/badge/support-buy%20me%20a%20coffee-FFDD00)](https://buymeacoffee.com/thomasgalliker)

Paging.NET is a lightweight and flexible library designed to simplify incremental server-side data loading. It provides an easy-to-use toolkit for efficiently managing large datasets by fetching data in small, controlled chunks. Ideal for applications that require smooth paging, reduced memory usage, and responsive data access patterns, Paging.NET helps you streamline your backend communication without unnecessary complexity.

### Download and Install Paging.NET
This library is available on NuGet: https://www.nuget.org/packages/Paging.NET/
Use the following command to install Paging using NuGet package manager console:

    PM> Install-Package Paging.NET

You can use this library in any .NET project which is compatible to .NET Standard 2.0 and higher as well as with .NET MAUI.


### Latest Releases
| Package | Version | Downlods |
|------|------|---|---|---|
| Paging.NET | [![Version](https://img.shields.io/nuget/v/Paging.NET.svg)](https://www.nuget.org/packages/Paging.NET) |[![Downloads](https://img.shields.io/nuget/dt/Paging.NET.svg)](https://www.nuget.org/packages/Paging.NET) |
| Paging.MAUI | [![Version](https://img.shields.io/nuget/v/Paging.MAUI.svg)](https://www.nuget.org/packages/Paging.MAUI) |[![Downloads](https://img.shields.io/nuget/dt/Paging.MAUI.svg)](https://www.nuget.org/packages/Paging.MAUI) |
| Paging.Queryable.NET | [![Version](https://img.shields.io/nuget/v/Paging.Queryable.NET.svg)](https://www.nuget.org/packages/Paging.Queryable.NET) |[![Downloads](https://img.shields.io/nuget/dt/Paging.Queryable.NET.svg)](https://www.nuget.org/packages/Paging.Queryable.NET) |

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
