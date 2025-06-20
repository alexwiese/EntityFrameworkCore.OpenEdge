using System;
using System.Data.Odbc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.Extensions.Configuration;

namespace EFCore.OpenEdge.FunctionalTests.TestUtilities
{
    public class OpenEdgeTestStore : RelationalTestStore
    {
        private static readonly Lazy<IConfiguration> _configuration =
            new(() =>
                new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .Build()
            );

        private static string GetConnectionString() => _configuration.Value.GetConnectionString("OpenEdgeConnection")
                                                       ?? throw new InvalidOperationException(
                                                           "OpenEdge connection string not found in appsettings.json");

        public static OpenEdgeTestStore GetOrCreate(string name) => new(name);
        public static OpenEdgeTestStore Create(string name) => new(name, shared: false);

        private OpenEdgeTestStore(string name, bool shared = true) : base(name, shared,
            new OdbcConnection(GetConnectionString()))
        {
        }

        public override DbContextOptionsBuilder AddProviderOptions(DbContextOptionsBuilder builder)
            => builder.UseOpenEdge(ConnectionString);
    }
}