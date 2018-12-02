using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;

namespace EntityFrameworkCore.OpenEdge.Storage.Internal
{
    public class OpenEdgeCommand : RelationalCommand
    {
        public OpenEdgeCommand(IDiagnosticsLogger<DbLoggerCategory.Database.Command> logger, string commandText, IReadOnlyList<IRelationalParameter> parameters) : base(logger, commandText, parameters)
        {
            Console.WriteLine("PARAMS: ");
            Console.WriteLine(parameters);
            
        }

        protected override Task<object> ExecuteAsync(IRelationalConnection connection, DbCommandMethod executeMethod, IReadOnlyDictionary<string, object> parameterValues,
            CancellationToken cancellationToken = new CancellationToken())
        {
            Console.WriteLine("PARAM VALUES: ");
            Console.WriteLine(parameterValues);
            
            return base.ExecuteAsync(connection, executeMethod, parameterValues, cancellationToken);
        }

        public override Task<int> ExecuteNonQueryAsync(IRelationalConnection connection, IReadOnlyDictionary<string, object> parameterValues,
            CancellationToken cancellationToken = new CancellationToken())
        {
            Console.WriteLine("ExecuteNonQueryAsync");
            return base.ExecuteNonQueryAsync(connection, parameterValues, cancellationToken);
        }

        public override Task<object> ExecuteScalarAsync(IRelationalConnection connection, IReadOnlyDictionary<string, object> parameterValues,
            CancellationToken cancellationToken = new CancellationToken())
        {
            Console.WriteLine("ExecuteScalarAsync");
            return base.ExecuteScalarAsync(connection, parameterValues, cancellationToken);
        }

        public override Task<RelationalDataReader> ExecuteReaderAsync(IRelationalConnection connection, IReadOnlyDictionary<string, object> parameterValues,
            CancellationToken cancellationToken = new CancellationToken())
        {
            //throw new Exception("WHERE AM I");
            Console.WriteLine("ExecuteReaderAsync");
            return base.ExecuteReaderAsync(connection, parameterValues, cancellationToken);
        }
    }
}