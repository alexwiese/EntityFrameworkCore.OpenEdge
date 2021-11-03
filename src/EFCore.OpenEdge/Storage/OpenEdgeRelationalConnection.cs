using Microsoft.EntityFrameworkCore.Storage;
using System.Data.Common;
using System.Data.Odbc;

namespace EntityFrameworkCore.OpenEdge.Storage
{
    public class OpenEdgeRelationalConnection : RelationalConnection, IOpenEdgeRelationalConnection
    {
        public OpenEdgeRelationalConnection(RelationalConnectionDependencies dependencies) : base(dependencies)
        {
        }

        protected override DbConnection CreateDbConnection() => new OdbcConnection(ConnectionString);
    }
}
