# Entity Framework Core provider for Progress OpenEdge

![Nuget](https://img.shields.io/nuget/v/EntityFrameworkCore.OpenEdge.svg)
![GitHub All Releases](https://img.shields.io/github/downloads/alexwiese/EntityFrameworkCore.OpenEdge/total.svg)


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
