# Entity Framework Core provider for Progress OpenEdge

[![Nuget](https://img.shields.io/nuget/v/EntityFrameworkCore.OpenEdge.svg)](https://www.nuget.org/packages/EntityFrameworkCore.OpenEdge)
[![Nuget](https://img.shields.io/nuget/dt/EntityFrameworkCore.OpenEdge.svg)](https://www.nuget.org/packages/EntityFrameworkCore.OpenEdge)
[![FOSSA Status](https://app.fossa.io/api/projects/git%2Bgithub.com%2Falexwiese%2FEntityFrameworkCore.OpenEdge.svg?type=shield)](https://app.fossa.io/projects/git%2Bgithub.com%2Falexwiese%2FEntityFrameworkCore.OpenEdge?ref=badge_shield)

EntityFrameworkCore.OpenEdge is an Entity Framework Core provider that allows you to use Entity Framework Core with Progress OpenEdge.

## Usage

### DSN-less Connection

    public class MyDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseOpenEdge("Driver=Progress OpenEdge 11.7 Driver;HOST=localhost;port=10000;UID=<user>;PWD=<password>;DIL=1;Database=<database>");
        }
    }

### Using a DSN

Create an ODBC DSN for your Progress OpenEdge database. Pass the connection string to the `UseOpenEdge` extension method.

    public class MyDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseOpenEdge("dsn=MyDb;password=mypassword");
        }
    }
 
## Scaffold/reverse engineer your model
 
From the Nuget Package Manager Console run this command (replacing the connection string).
 
     Scaffold-DbContext "dsn=MyDb;password=mypassword" EntityFrameworkCore.OpenEdge -OutputDir Models
     
     
## What's working?

### Core Query Operations
- **Basic Queries**: SELECT, WHERE, ORDER BY, COUNT, FIRST
- **Aggregations**: COUNT, SUM, GROUP BY operations
- **Subqueries**: Subqueries in WHERE clauses, EXISTS and NOT EXISTS patterns
- **Paging**: SKIP and TAKE operations for pagination
- **Sorting**: Multiple ORDER BY clauses with ASC/DESC

### Join Operations
- **Inner Joins**: Simple and complex multi-table joins
- **Left Outer Joins**: LEFT JOIN with DefaultIfEmpty patterns
- **Navigation Properties**: Include operations with filtered includes
- **Complex Joins**: Multi-table joins across related entities

### String Operations
- **String Methods**: Contains, StartsWith, EndsWith
- **String Comparisons**: Equality and pattern matching

### Mathematical Operations
- **Arithmetic**: Basic math operations (+, -, *, /) on decimal values
- **Math Functions**: Math.Round, Math.Abs
- **Calculations**: Discount calculations and price manipulations

### Additional Features
- **Null Handling**: Proper NULL checks and comparisons
- **Type Mapping**: OpenEdge-specific type mappings including LOGICAL types

### Data Manipulation
- Inserts
- Updates
- Deletes
- Scaffolding

## Gotchas

OpenEdge Databases are a bit different when it comes to primary keys. Ie. there aren’t any “real” primary keys. There are primary indexes but they do _not_ have to be unique which causes issues with EFCore (which the provider can’t circumvent). EFCore entity tracking requires all primary keys to be unique, otherwise the materialised entity objects will conflict. The only thing that is close to a primary key (if there is no unique, primary index available) in OpenEdge is the “rowid”. You can expose the rowid and use that as the primary key.

Example:

    [Key]
    [Column("rowid")]
    public string Rowid { get; set; }
    
Note that rowid is a special OpenEdge value that uniquely represents the record, this means you can add this to any OpenEdge entity and use it as a proper primary key.

For a unique primary index that has multiple fields then you can do the following in OnModelCreating:

    modelBuilder.Entity<transaction>().HasKey("TransactionId", "ClientId", "SecondaryId");

## TODO
Implement the provider tests according to the specification.  
Here's example of the test suite from SQL provider: [EFCore.SqlServer.FunctionalTests Github](https://github.com/dotnet/efcore/tree/main/test/EFCore.SqlServer.FunctionalTests).
For more information, here's [Microsoft docs page](https://learn.microsoft.com/en-us/ef/core/providers/writing-a-provider#the-ef-core-specification-tests).

## License
[![FOSSA Status](https://app.fossa.io/api/projects/git%2Bgithub.com%2Falexwiese%2FEntityFrameworkCore.OpenEdge.svg?type=large)](https://app.fossa.io/projects/git%2Bgithub.com%2Falexwiese%2FEntityFrameworkCore.OpenEdge?ref=badge_large)
