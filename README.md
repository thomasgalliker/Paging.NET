# Paging.NET
[![Version](https://img.shields.io/nuget/v/Paging.NET.svg)](https://www.nuget.org/packages/Paging.NET)  [![Downloads](https://img.shields.io/nuget/dt/Paging.NET.svg)](https://www.nuget.org/packages/Paging.NET)

<img src="https://github.com/thomasgalliker/Paging.NET/raw/master/logo.png" height="100" alt="Paging.NET" align="right">
Paging provides tools which are commonly used for handling paged data loading.

### Download and Install Paging.NET
This library is available on NuGet: https://www.nuget.org/packages/Paging.NET/
Use the following command to install Paging using NuGet package manager console:

    PM> Install-Package Paging.NET

You can use this library in any .Net project which is compatible to .Net Standard 1.0 and higher as well as with legacy PCL projects.


### Latest Builds

|   | Paging.NET| Paging.Forms.NET | Paging.Queryable.NET |
| - | ---------- | ---------------- | -------------------- |
|Version| [![Version](https://img.shields.io/nuget/v/Paging.NET.svg)](https://www.nuget.org/packages/Paging.NET) | [![Version](https://img.shields.io/nuget/v/Paging.Forms.NET.svg)](https://www.nuget.org/packages/Paging.Forms.NET) | [![Version](https://img.shields.io/nuget/v/Paging.Queryable.NET.svg)](https://www.nuget.org/packages/Paging.Queryable.NET)
|Pre-Release| [![Version](https://img.shields.io/nuget/vpre/Paging.NET.svg)](https://www.nuget.org/packages/Paging.NET) | [![Version](https://img.shields.io/nuget/vpre/Paging.Forms.NET.svg)](https://www.nuget.org/packages/Paging.Forms.NET) | [![Version](https://img.shields.io/nuget/vpre/Paging.Queryable.NET.svg)](https://www.nuget.org/packages/Paging.Queryable.NET)
|Downloads| [![Downloads](https://img.shields.io/nuget/dt/Paging.NET.svg)](https://www.nuget.org/packages/Paging.NET) | [![Downloads](https://img.shields.io/nuget/dt/Paging.Forms.NET.svg)](https://www.nuget.org/packages/Paging.Forms.NET) | [![Downloads](https://img.shields.io/nuget/dt/Paging.Queryable.NET.svg)](https://www.nuget.org/packages/Paging.Queryable.NET)

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

### License
This project is Copyright &copy; 2019 [Thomas Galliker](https://ch.linkedin.com/in/thomasgalliker). Free for non-commercial use. For commercial use please contact the author.
