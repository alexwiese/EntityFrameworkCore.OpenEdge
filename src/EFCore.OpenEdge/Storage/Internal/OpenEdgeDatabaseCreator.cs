using System;
using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.OpenEdge.Storage.Internal
{
    /// <summary>
    /// Performs database/schema creation, and other related operations.
    /// </summary>
    public class OpenEdgeDatabaseCreator : RelationalDatabaseCreator
    {
        // TODO: Double check what all of this is about
        public OpenEdgeDatabaseCreator(RelationalDatabaseCreatorDependencies dependencies) : base(dependencies)
        {
        }

        public override bool Exists()
        {
            try
            {
                // Try to open a connection to check if database exists
                using var connection = Dependencies.Connection.DbConnection;
                connection.Open();
                connection.Close();
                return true;
            }
            catch
            {
                // If connection fails, assume database doesn't exist
                return false;
            }
        }

        public override void Create()
        {
            // For the moment being, enforce database creation using file-system based tools
            throw new NotSupportedException("OpenEdge databases must be created externally using OpenEdge tools.");
        }

        public override void Delete()
        {
            // For the moment being, enforced to be handled externally
            throw new NotSupportedException("OpenEdge databases should be deleted using OpenEdge tools.");
        }

        public override bool HasTables()
        {
            try
            {
                using var connection = Dependencies.Connection.DbConnection;
                connection.Open();
                
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT COUNT(*) 
                    FROM SYSPROGRESS.SYSTABLES";
                
                var result = command.ExecuteScalar();
                connection.Close();
                
                return Convert.ToInt32(result) > 0;
            }
            catch
            {
                // If we can't determine, assume no tables
                return false;
            }
        }
    }
}