# Entity Framework Core provider for Progress OpenEdge

[![Nuget](https://img.shields.io/nuget/v/EntityFrameworkCore.OpenEdge.svg)](https://www.nuget.org/packages/EntityFrameworkCore.OpenEdge)
[![Nuget](https://img.shields.io/nuget/dt/EntityFrameworkCore.OpenEdge.svg)](https://www.nuget.org/packages/EntityFrameworkCore.OpenEdge)

EntityFrameworkCore.OpenEdge is an Entity Framework Core provider that allows you to use Entity Framework Core with Progress OpenEdge.

## Usage

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

- Basic Queries
- Joins
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
