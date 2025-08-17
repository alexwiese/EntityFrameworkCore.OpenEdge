# EntityFrameworkCore.OpenEdge
[![NuGet Version](https://img.shields.io/nuget/v/EntityFrameworkCore.OpenEdge)](https://www.nuget.org/packages/EntityFrameworkCore.OpenEdge)
[![License: Apache-2.0](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](LICENSE)

EntityFrameworkCore.OpenEdge is an **Entity Framework Core 9 provider** that lets you target Progress OpenEdge databases via **ODBC**.

> ⚠️ **Status:** This library is under active development. While it is already used in production scenarios, you may still encounter bugs or missing edge-cases. Please [open an issue](https://github.com/alexwiese/EntityFrameworkCore.OpenEdge/issues) if you run into problems.

---

## Quick Start

### Install
```bash
dotnet add package EntityFrameworkCore.OpenEdge --version 9.0.4
```

### DSN-less connection
```csharp
optionsBuilder.UseOpenEdge(
    "Driver=Progress OpenEdge 11.7 Driver;" +
    "HOST=localhost;PORT=10000;UID=<user>;PWD=<password>;DIL=1;Database=<db>");
```

### Using a DSN
```csharp
optionsBuilder.UseOpenEdge("dsn=MyDb;password=mypassword");
```

### Custom schema (defaults to "pub")
```csharp
optionsBuilder.UseOpenEdge(
    connectionString: "dsn=MyDb;password=mypassword",
    defaultSchema: "myschema");
```

### Reverse-engineer an existing database
```powershell
Scaffold-DbContext "dsn=MyDb;password=mypassword" EntityFrameworkCore.OpenEdge -OutputDir Models
```

---

## Feature Matrix (EF Core 9)

| Area                     | Status | Notes                                                                    |
|--------------------------|:------:|---------------------------------------------------------------------------|
| Queries                  | ✅     | `SELECT`, `WHERE`, `ORDER BY`, `GROUP BY`, paging (`Skip`/`Take`), aggregates |
| Joins / `Include`        | ✅     | `INNER JOIN`, `LEFT JOIN`, filtered `Include`s                            |
| String operations        | ✅     | `Contains`, `StartsWith`, `EndsWith`, `Length`                           |
| CRUD                     | ✅     | `INSERT`, `UPDATE`, `DELETE` – one command per operation (OpenEdge limitation) |
| Scaffolding              | ✅     | `Scaffold-DbContext`                                                     |
| Nested queries           | ✅     | `Skip`/`Take` inside sub-queries (new in 9.0.4)                           |
| DateTime literal support | ✅     | `{ ts 'yyyy-MM-dd HH:mm:ss' }` formatting                                |

---

## OpenEdge Gotchas

* **Primary keys** – OpenEdge doesn’t enforce uniqueness. Use `rowid` or define composite keys
* **No batching / `RETURNING`** – each modification executes individually; concurrency detection is limited.

See the [OpenEdge SQL Reference](https://docs.progress.com/bundle/openedge-sql-reference/) for database specifics.

## Legacy 1.x Line (netstandard2.0 / EF Core 2.1)

The original **1.x** versions target **netstandard 2.0** and EF Core 2.1.  
They remain on NuGet for applications that cannot yet migrate to .NET 8/9.

| Package | Framework | EF Core | Install |
|---------|-----------|---------|---------|
| **1.0.11** (latest stable) | netstandard2.0 | 2.1.x | `dotnet add package EntityFrameworkCore.OpenEdge --version 1.0.11` |
| 1.0.12-rc3 | netstandard2.0 | 2.1.x | `dotnet add package EntityFrameworkCore.OpenEdge --version 1.0.12-rc3` |

The 1.x branch is **feature-frozen**. New development happens in the 9.x line.  

## Contributing & Development

We welcome pull requests — especially **back-ports to older EF Core branches** and **additional translator implementations**.

* **Testing requirements:** EF Core providers are expected to pass the [EF Core provider specification tests](https://learn.microsoft.com/ef/core/providers/writing-a-provider). Our current test harness is **minimal** and may not cover all cases. Contributions that add missing tests (or implement the snadard testing flow for database providers) are highly appreciated.
* **Bug reports:** Please include a runnable repro or failing test where possible.

For a deeper dive into the architecture (query / update pipelines, type mapping, etc.) browse the source under `src/EFCore.OpenEdge`.

## License

Apache-2.0 – see [LICENSE](LICENSE).
