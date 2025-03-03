﻿# ![Logo](https://raw.githubusercontent.com/ceronus/fspquery/master/doc/icons/icon-64x64.png) FspQuery
FspQuery (filter-sort-page-query a.k.a "fspq") is a powerful and lightweight library designed to simplify filtering, sorting, and paging for structured queries.
It offers seamless integration with LINQ-based queries and provides an intuitive API for working with large datasets efficiently.

[![NuGet version (FspQuery)](https://img.shields.io/nuget/v/fspquery?style=for-the-badge)](https://www.nuget.org/packages/fspquery/)  
[![Build](https://github.com/ceronus/fspquery/actions/workflows/ci.yml/badge.svg)](https://github.com/ceronus/fspquery/actions/workflows/ci.yml)

## Repository
[GitHub Repository](https://github.com/ceronus/filter-sort-page-query)


## Features
- ✅ **Filtering**: Apply flexible conditions to refine results.
- ✅ **Sorting**: Easily sort data by specified properties.
- ✅ **Paging**: Efficiently paginate large datasets.
- ✅ **Validation Support**: Built-in query validation to ensure correctness of filters and pagination parameters.
- ✅ **LINQ-Friendly**: Works seamlessly with `IQueryable<T>` collections.
- ✅ **Easy Integration**: Works effortlessly with APIs and HTTP query strings.
- ✅ **CosmosDB-Compatible**: Optimized for querying Azure CosmosDB databases.

## Installation
You can install FspQuery via NuGet:

```sh
dotnet add package FspQuery
```

Or via the .NET CLI:

```sh
nuget install FspQuery
```

## Usage

### Extension Methods
FspQuery provides extension methods for `IQueryable<T>` to apply filtering, sorting, and paging operations efficiently.

#### Basic Usage
```csharp
using FspQuery;

ObjectIndexer indexer = new();
FspQueryLogic fspLogic = new(indexer);

IFspQueryOptions fspOptions = new FooRequest()
{
  PageNumber = 2,
  PageSize = 50,
  SortPropertyName = "lastModifiedTimestamp",
  SortDirection = ListSortDirection.Descending,
  Filters =
  [
    FilterCondition.CreateNotContains("email.address", "@apple")
  ]
};

IQueryable<T> data;
IQueryable<T> fspData = data.ApplyPagingFilteringSorting(fspLogic, fspOptions);
```

#### Available Extensions
```csharp
data.ApplyPagingFilteringSorting(fspLogic, fspOptions);
data.ApplyPaging(fspLogic, fspOptions);
data.ApplyFiltering(fspLogic, fspOptions);
data.ApplySorting(fspLogic, fspOptions);
```

## Dependency Injection
You can add it to your project using dependency injection:
```csharp
services.AddFspQuery();
```



### Query Validation
FspQuery includes a validation mechanism to ensure query parameters are well-formed and do not contain invalid values.

#### Using the Validator
```csharp
using FspQuery;

ObjectIndexer indexer = new();
FspQueryValidator validator = new(indexer);
string? errorMessage;

if (!validator.ValidateWithDefaults<MyEntity>(fspQueryOptions, out errorMessage))
{
    Console.WriteLine($"Validation failed: {errorMessage}");
}
```


### Query String Parsing in FspQuery

FspQuery provides powerful query string parsing to enable flexible filtering, sorting, and paging through HTTP request parameters. Below is a breakdown of the special prefixes used to apply various filter conditions to properties.

#### Query String Prefix Mapping

| Prefix  | Description                                | Example Query Parameter          |
|---------|--------------------------------------------|----------------------------------|
| `eq^`   | Equals                                     | `eq^name=John`                   |
| `!eq^`  | Not Equals                                 | `!eq^name=John`                  |
| `in^`   | Contains                                   | `in^title=Data`                  |
| `!in^`  | Not Contains                               | `!in^title=Data`                 |
| `pre^`  | Starts With                                | `pre^category=Science`           |
| `!pre^` | Not Starts With                            | `!pre^category=Science`          |
| `end^`  | Ends With                                  | `end^filename=.pdf`              |
| `!end^` | Not Ends With                              | `!end^filename=.pdf`             |
| `gt^`   | Greater Than                               | `gt^price=50`                    |
| `!gt^`  | Not Greater Than                           | `!gt^price=50`                   |
| `gte^`  | Greater Than or Equal                      | `gte^price=50`                   |
| `!gte^` | Not Greater Than or Equal                  | `!gte^price=50`                  |
| `lt^`   | Less Than                                  | `lt^price=100`                   |
| `!lt^`  | Not Less Than                              | `!lt^price=100`                  |
| `lte^`  | Less Than or Equal                         | `lte^price=100`                  |
| `!lte^` | Not Less Than or Equal                     | `!lte^price=100`                 |

#### Example Query String

To filter a dataset where `category` equals `Books`, `price` is greater than or equal to `10`, and the `title` contains `Science`, the query string would look like this:

```
?page=1&pagesize=20&sort=name&order=asc&eq^category=Books&gte^price=10&in^title=Science
```

#### How to Parse Query Strings in FspQuery

FspQuery automatically parses query strings into structured filter conditions. Here's an example of how to handle query string parsing:

```csharp
using FspQuery;
using Microsoft.AspNetCore.Http;

IQueryCollection queryCollection = httpContext.Request.Query;
IFspQueryOptions fspOptions = new FspQueryOptions();

if (queryCollection.TryParse(fspOptions, out string? errorMessage))
{
    Console.WriteLine("Query parsed successfully");
}
else
{
    Console.WriteLine($"Error parsing query: {errorMessage}");
}
```

This feature ensures seamless integration with API endpoints by converting query parameters into structured query logic automatically.






### Extension Methods
FspQuery provides extension methods for `IQueryable<T>` to apply filtering, sorting, and paging operations efficiently.

#### Basic Usage
```csharp
using FspQuery;

var filteredData = data.ApplyPagingFilteringSorting(fspQueryLogic, fspQueryOptions);
```

#### Available Extensions
```csharp
public static class IQueryableExtensions
{
    public static IQueryable<T> ApplyPagingFilteringSorting<T>(this IQueryable<T> query, IFspQueryLogic logic, IFspQueryOptions operation) where T : class
        => logic.ApplyFilteringSortingPaging(query, operation);

    public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> query, IFspQueryLogic logic, IFspQueryOptions operation) where T : class
        => logic.ApplyPaging(query, operation);

    public static IQueryable<T> ApplyFiltering<T>(this IQueryable<T> query, IFspQueryLogic logic, IFspQueryOptions operation) where T : class
        => logic.ApplyFiltering(query, operation);

    public static IQueryable<T> ApplySorting<T>(this IQueryable<T> query, IFspQueryLogic logic, IFspQueryOptions operation) where T : class
        => logic.ApplySorting(query, operation);
}
```





## License
This project is licensed under the [MIT License.](LICENSE.md)

---
🚀 **FspQuery** makes filtering, sorting, and paging effortless.









