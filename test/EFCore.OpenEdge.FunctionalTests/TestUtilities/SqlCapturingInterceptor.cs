using System.Data;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EFCore.OpenEdge.FunctionalTests.TestUtilities
{
    public class SqlCapturingInterceptor : DbCommandInterceptor
    {
        private readonly List<string> _capturedSql = new();
        private readonly List<DbParameter[]> _capturedParameters = new();

        public IReadOnlyList<string> CapturedSql => _capturedSql;
        public IReadOnlyList<DbParameter[]> CapturedParameters => _capturedParameters;

        public void Clear()
        {
            _capturedSql.Clear();
            _capturedParameters.Clear();
        }

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            CaptureCommand(command);
            return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            CaptureCommand(command);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override InterceptionResult<int> NonQueryExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result)
        {
            CaptureCommand(command);
            return base.NonQueryExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            CaptureCommand(command);
            return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override InterceptionResult<object> ScalarExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<object> result)
        {
            CaptureCommand(command);
            return base.ScalarExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<object> result,
            CancellationToken cancellationToken = default)
        {
            CaptureCommand(command);
            return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
        }

        private void CaptureCommand(DbCommand command)
        {
            _capturedSql.Add(command.CommandText);
            
            var parameters = new DbParameter[command.Parameters.Count];
            for (int i = 0; i < command.Parameters.Count; i++)
            {
                var param = command.Parameters[i];
                parameters[i] = new CapturedParameter
                {
                    ParameterName = param.ParameterName,
                    Value = param.Value,
                    DbType = param.DbType,
                    Direction = param.Direction
                };
            }
            _capturedParameters.Add(parameters);
        }
    }

    public class CapturedParameter : DbParameter
    {
        public override DbType DbType { get; set; }
        public override ParameterDirection Direction { get; set; }
        public override bool IsNullable { get; set; }
        public override string ParameterName { get; set; }
        public override int Size { get; set; }
        public override string SourceColumn { get; set; }
        public override bool SourceColumnNullMapping { get; set; }
        public override object Value { get; set; }

        public override void ResetDbType() {}
    }
}
