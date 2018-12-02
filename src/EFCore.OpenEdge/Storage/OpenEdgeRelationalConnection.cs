using System.Data.Common;
using System.Data.Odbc;
using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.OpenEdge.Storage
{
    public class OpenEdgeRelationalConnection : RelationalConnection, IOpenEdgeRelationalConnection
    {
        public OpenEdgeRelationalConnection(RelationalConnectionDependencies dependencies) 
            : base(dependencies)
        {
        }

        protected override DbConnection CreateDbConnection()
            => new OdbcConnection(ConnectionString);
    }
}