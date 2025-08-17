using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using Xunit;

namespace EFCore.OpenEdge.FunctionalTests.TestUtilities
{
    public abstract class OpenEdgeTestBase : IDisposable
    {
        protected IConfiguration Configuration { get; }
        protected ServiceProvider ServiceProvider { get; }
        protected string ConnectionString { get; }
        
        private readonly ILoggerFactory _loggerFactory;
        
        protected OpenEdgeTestBase()
        {
            Configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            ConnectionString = Configuration.GetConnectionString("OpenEdgeConnection");
            
            _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            
            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();
        }

        protected void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(Configuration);
        }

        protected DbContextOptionsBuilder<T> CreateOptionsBuilder<T>() where T : DbContext
        {
            return new DbContextOptionsBuilder<T>()
                .UseOpenEdge(ConnectionString, "PUB")
                .EnableSensitiveDataLogging()
                .UseLoggerFactory(_loggerFactory);
        }

        protected DbContextOptions CreateOptions()
        {
            return new DbContextOptionsBuilder()
                .UseOpenEdge(ConnectionString, "PUB")
                .EnableSensitiveDataLogging()
                .UseLoggerFactory(_loggerFactory)
                .Options;
        }

        public virtual void Dispose()
        {
            ServiceProvider?.Dispose();
        }
    }
}