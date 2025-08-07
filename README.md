# Entity Framework Core provider for Progress OpenEdge

[![NuGet Version](https://img.shields.io/nuget/v/EntityFrameworkCore.OpenEdge.Extended)](https://www.nuget.org/packages/EntityFrameworkCore.OpenEdge.Extended/) [![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2Ferudzitis%2FEntityFrameworkCore.OpenEdge.svg?type=shield&issueType=license)](https://app.fossa.com/projects/git%2Bgithub.com%2Ferudzitis%2FEntityFrameworkCore.OpenEdge?ref=badge_shield&issueType=license) [![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2Ferudzitis%2FEntityFrameworkCore.OpenEdge.svg?type=shield&issueType=security)](https://app.fossa.com/projects/git%2Bgithub.com%2Ferudzitis%2FEntityFrameworkCore.OpenEdge?ref=badge_shield&issueType=security)

EntityFrameworkCore.OpenEdge is an Entity Framework Core provider that allows you to use Entity Framework Core with Progress OpenEdge databases through ODBC connections. This provider supports EF Core 9.

## Features

* **Querying**: `SELECT`, `WHERE`, `ORDER BY`, `GROUP BY`, `SKIP`/`TAKE` (paging), `COUNT`, `SUM`, `FIRST`
* **Joins**: `INNER JOIN`, `LEFT JOIN`, `Include` for navigation properties, filtered includes
* **String Operations**: `Contains`, `StartsWith`, `EndsWith` (translated to `LIKE`), `Length` property
* **Data Manipulation**: `INSERT`, `UPDATE`, `DELETE` operations with OpenEdge-optimized SQL generation
* **Scaffolding**: Reverse engineering of existing OpenEdge database schemas (Database First)

## Getting Started

### Installation

Install the NuGet package:

```bash
dotnet add package EntityFrameworkCore.OpenEdge.Extended --version 9.0.3
```

### Configuration

#### DSN-less Connection

```csharp
public class MyDbContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseOpenEdge("Driver=Progress OpenEdge 11.7 Driver;HOST=localhost;port=10000;UID=<user>;PWD=<password>;DIL=1;Database=<database>");
    }
}
```

#### Using a DSN

Create an ODBC DSN for your Progress OpenEdge database:

```csharp
public class MyDbContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseOpenEdge("dsn=MyDb;password=mypassword");
    }
}
```

#### Custom Schema Configuration

By default, the provider uses the `"pub"` schema. You can specify a different default schema:

```csharp
public class MyDbContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Using custom schema (defaults to "pub" if not specified)
        optionsBuilder.UseOpenEdge(
            connectionString: "dsn=MyDb;password=mypassword",
            defaultSchema: "myschema");
    }
}
```

**Note**: The schema parameter affects table name resolution when tables don't have explicitly defined schemas in your entity configurations.

### Database First Development (Scaffolding)

Reverse engineer an existing OpenEdge database:

```bash
Scaffold-DbContext "dsn=MyDb;password=mypassword" EntityFrameworkCore.OpenEdge -OutputDir Models
```

## OpenEdge Specifics & Gotchas

### Primary Keys and `rowid`

OpenEdge databases don't have true primary keys. Primary indexes exist but are not required to be unique, which conflicts with EF Core's entity tracking requirements. The `rowid` is your best option for a reliable primary key:

```csharp
[Key]
[Column("rowid")]
public string Rowid { get; set; }
```

For composite primary keys using unique indexes:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Transaction>().HasKey("TransactionId", "ClientId", "SecondaryId");
}
```

### Boolean Logic

OpenEdge SQL requires explicit boolean comparisons (e.g., `WHERE IsActive = 1`). The provider automatically handles this:

```csharp
// This C# code:
var activeUsers = context.Users.Where(u => u.IsActive).ToList();

// Becomes this SQL:
// SELECT * FROM "Users" WHERE "IsActive" = 1
```

### Paging with `Skip()` and `Take()`

OpenEdge requires literal values for `OFFSET` and `FETCH` clauses. The provider automatically inlines these values rather than using parameters:

```csharp
// This code:
var pagedResults = context.Users.Skip(10).Take(20).ToList();

// Generates:
// SELECT * FROM "Users" OFFSET 10 ROWS FETCH NEXT 20 ROWS ONLY
```

### No Update Batching

Each `INSERT`, `UPDATE`, and `DELETE` operation executes individually. Multiple changes in a single `SaveChanges()` call result in multiple database round trips.

### Parameter Handling

The provider uses positional `?` parameters instead of named parameters, carefully managing parameter order to match SQL placeholders.

## Architecture Highlights

### Query Pipeline

LINQ queries are translated into OpenEdge-compatible SQL with several key customizations:

- **Boolean Handling**: Implicit boolean checks are converted to explicit comparisons (`WHERE "IsActive" = 1`)
- **String Functions**: `.Contains()`, `.StartsWith()`, and `.EndsWith()` translate to SQL `LIKE` expressions using `CONCAT` functions
- **Paging**: `Skip()` and `Take()` values are inlined as literals into `OFFSET`/`FETCH` clauses
- **Parameters**: Uses positional `?` parameters instead of named parameters

### Update Pipeline

The update pipeline handles OpenEdge's specific requirements:

- **Single Command Execution**: Each modification is processed individually in its own command batch
- **No `RETURNING` Support**: OpenEdge doesn't support RETURNING clauses, affecting concurrency detection and identity retrieval
- **Parameter Ordering**: Carefully orders parameters to match positional `?` placeholders in generated SQL
- **DateTime Formatting**: Uses ODBC timestamp escape sequences `{ ts 'yyyy-MM-dd HH:mm:ss' }` for datetime literals, which OpenEdge requires for proper datetime handling

## Query Pipeline Deep Dive

For developers interested in the technical details of how LINQ queries become SQL:

### Query Translation Process

1. **LINQ Expression Tree**: C# LINQ methods create a .NET expression tree
2. **Queryable Method Translation**: `OpenEdgeQueryableMethodTranslatingExpressionVisitor` converts high-level operations (Where, OrderBy, Skip, Take) into relational expressions
3. **SQL Expression Translation**: `OpenEdgeSqlTranslatingExpressionVisitor` handles:
   - String methods (StartsWith → LIKE 'pattern%')
   - Boolean properties (IsActive → IsActive = 1)  
   - Member access (string.Length → LENGTH())
4. **Post-processing**: `OpenEdgeQueryTranslationPostprocessor` validates and optimizes the query tree
5. **Parameter Processing**: `OpenEdgeParameterBasedSqlProcessor` converts OFFSET/FETCH parameters to literal values
6. **SQL Generation**: `OpenEdgeSqlGenerator` produces the final SQL with:
   - Positional `?` parameters
   - OFFSET/FETCH syntax for pagination
   - Boolean CASE expressions in projections
   - OpenEdge-specific datetime literals

## Known Limitations

### OpenEdge Database Constraints

These limitations stem from OpenEdge database architecture and SQL dialect, which the provider correctly handles:

#### Update Operations
- **No Batching Support**: OpenEdge doesn't support batching multiple modifications in a single command
- **No RETURNING Clause**: OpenEdge SQL doesn't support RETURNING for retrieving generated values or affected row counts
- **ODBC Parameter Format**: Requires positional `?` placeholders instead of named parameters like `@param1`
- **Limited Concurrency Features**: Optimistic concurrency detection is constrained by the lack of RETURNING support

For specific OpenEdge SQL capabilities, consult the [OpenEdge SQL Reference](https://docs.progress.com/bundle/openedge-sql-reference/).

## Contributing & Development

### Repository Background

This repository is a fork and modernization of an older OpenEdge EF Core provider originally written for .NET Framework 2.1. The current implementation represents a significant evolution with proper architecture, comprehensive type mappings, and OpenEdge-specific optimizations.

### Branch Structure & Status

The repository maintains multiple branches for different EF Core versions:

| Branch | EF Core Version | Status | Description |
|--------|----------------|--------|--------------|
| `master` | 9.x | ✅ **Complete** | Fully validated with all essential features |
| `efcore8` | 8.x | ⚠️ **Needs Features** | Basic migration complete, missing essential features |
| `efcore6` | 6.x | ⚠️ **Needs Features** | Basic migration complete, missing essential features |
| `efcore5` | 5.x | ⚠️ **Needs Features** | Basic migration complete, missing essential features |
| `efcore3.1` | 3.1.x | ⚠️ **Needs Features** | Basic migration complete, missing essential features |

### How to Contribute

#### Priority: Feature Backporting

The most critical contribution needed is **backporting essential features** from the `master` (EF Core 9) branch to older versions

**Backporting Process:**
1. Check out the target branch (e.g., `efcore8`)
2. Compare implementations with `master` branch
3. Adapt the features to the target EF Core version's API
4. Ensure all tests pass
5. Submit a pull request

#### Future Development Priorities

**Query Translation Enhancements:**
- **Method/Member Translators**: Evaluate need for additional translators (Math functions, DateTime operations, etc.)
- **Nested Query Optimizations**: Improve performance for complex subqueries and navigation property includes
- **Aggregate Function Support**: Expand beyond basic COUNT/SUM operations

**Performance & Reliability:**
- **Bulk Operations**: Investigate workarounds for OpenEdge's single-command limitation
- **Connection Pooling**: Optimize ODBC connection management
- **Error Handling**: Improve OpenEdge-specific error message translation

### Testing Requirements

This project needs comprehensive test coverage following EF Core provider specifications:

**Required Test Coverage:**
- Query translation tests for all supported LINQ operations
- Update pipeline tests (INSERT/UPDATE/DELETE scenarios)
- Type mapping validation tests
- OpenEdge-specific feature tests (boolean handling, schema support, etc.)
- Cross-version compatibility tests for each EF Core branch

**Testing Resources:**
- [EFCore.SqlServer.FunctionalTests](https://github.com/dotnet/efcore/tree/main/test/EFCore.SqlServer.FunctionalTests) for reference implementation
- [Microsoft's provider testing documentation](https://learn.microsoft.com/en-us/ef/core/providers/writing-a-provider#the-ef-core-specification-tests)

## License

[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2Ferudzitis%2FEntityFrameworkCore.OpenEdge.svg?type=shield&issueType=license)](https://app.fossa.com/projects/git%2Bgithub.com%2Ferudzitis%2FEntityFrameworkCore.OpenEdge?ref=badge_shield&issueType=license)
